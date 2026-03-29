// src/Inventory/Inventory.Api/Handlers/Events/OrderPlacedHandler.cs
using Contracts.Events;
using Contracts.Models;
using Inventory.Api.Domain;
using Marten;
using Wolverine;

namespace Inventory.Api.Handlers.Events;

// Pure logic extracted for testability
public static class OrderPlacedHandlerLogic
{
    public static (bool success, StockReserved? reserved, StockReservationFailed? failed,
        Dictionary<Guid, int> updatedQuantities)
        TryReserve(Guid orderId, OrderItem[] orderItems, IEnumerable<InventoryItem> items)
    {
        var itemLookup = items.ToDictionary(i => i.Id);
        var updatedQuantities = new Dictionary<Guid, int>();

        foreach (var orderItem in orderItems)
        {
            if (!itemLookup.TryGetValue(orderItem.ItemId, out var item) ||
                !item.HasSufficientStock(orderItem.Quantity))
            {
                var reason = $"Insufficient stock for '{orderItem.ItemName}' " +
                             $"(requested: {orderItem.Quantity}, available: {item?.StockQuantity ?? 0})";
                return (false, null, new StockReservationFailed(orderId, reason), updatedQuantities);
            }
            updatedQuantities[orderItem.ItemId] = item.StockQuantity - orderItem.Quantity;
        }

        return (true, new StockReserved(orderId, orderItems), null, updatedQuantities);
    }
}

public static class OrderPlacedHandler
{
    public static async Task Handle(
        OrderPlaced evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        // NOTE: Not idempotent — duplicate delivery of OrderPlaced will fail the stock check
        // on already-reduced quantities. Acceptable for this POC.
        // Load all inventory items involved in the order
        var itemIds = evt.Items.Select(i => i.ItemId).ToArray();
        var items = new List<InventoryItem>();

        foreach (var itemId in itemIds)
        {
            var item = await session.Events.AggregateStreamAsync<InventoryItem>(itemId);
            if (item is not null) items.Add(item);
        }

        var (success, reserved, failed, updatedQtys) =
            OrderPlacedHandlerLogic.TryReserve(evt.OrderId, evt.Items, items);

        if (!success)
        {
            await bus.PublishAsync(failed!);
            return;
        }

        // Append StockQuantityUpdated events to each item's stream and broadcast to read model
        foreach (var (itemId, newQty) in updatedQtys)
        {
            var stockUpdate = new StockQuantityUpdated(itemId, newQty);
            session.Events.Append(itemId, stockUpdate);
            await bus.PublishAsync(stockUpdate);
        }

        await bus.PublishAsync(reserved!);
        // Wolverine's AutoApplyTransactions calls SaveChangesAsync after this handler completes
    }
}
