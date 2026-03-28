// src/Inventory/Inventory.Api/Handlers/Events/PaymentFailedHandler.cs
using Contracts.Events;
using Inventory.Api.Domain;
using Marten;
using Wolverine;

namespace Inventory.Api.Handlers.Events;

public static class PaymentFailedHandler
{
    public static async Task Handle(
        PaymentFailed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        // Restore stock for each item in the failed order
        foreach (var orderItem in evt.Items)
        {
            var item = await session.Events.AggregateStreamAsync<InventoryItem>(orderItem.ItemId);
            if (item is null) continue;

            var restoredQty = item.StockQuantity + orderItem.Quantity;
            session.Events.Append(orderItem.ItemId, new StockQuantityUpdated(orderItem.ItemId, restoredQty));
        }

        await bus.PublishAsync(new StockReleased(evt.OrderId, evt.Items));
        // Wolverine's AutoApplyTransactions calls SaveChangesAsync after this handler completes
    }
}
