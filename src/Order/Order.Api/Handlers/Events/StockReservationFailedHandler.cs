// src/Order/Order.Api/Handlers/Events/StockReservationFailedHandler.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Order.Api.Handlers.Events;

public static class StockReservationFailedHandler
{
    public static async Task Handle(
        StockReservationFailed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var failed = new OrderFailed(evt.OrderId, evt.Reason);
        session.Events.Append(evt.OrderId, failed);
        await bus.PublishAsync(failed);
    }
}
