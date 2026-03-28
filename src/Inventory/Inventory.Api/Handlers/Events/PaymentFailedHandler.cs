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
        // NOTE: Not idempotent — duplicate delivery would double-restore stock. Acceptable for this POC.
        var restoredItems = new List<Contracts.Models.OrderItem>();

        foreach (var orderItem in evt.Items)
        {
            var item = await session.Events.AggregateStreamAsync<InventoryItem>(orderItem.ItemId);
            if (item is null) continue;

            var restoredQty = item.StockQuantity + orderItem.Quantity;
            session.Events.Append(orderItem.ItemId, new StockQuantityUpdated(orderItem.ItemId, restoredQty));
            restoredItems.Add(orderItem);
        }

        if (restoredItems.Count > 0)
            await bus.PublishAsync(new StockReleased(evt.OrderId, restoredItems.ToArray()));
        // Wolverine's AutoApplyTransactions calls SaveChangesAsync after this handler completes
    }
}
