// src/Order/Order.Api/Handlers/Events/PaymentFailedHandler.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Order.Api.Handlers.Events;

public static class PaymentFailedHandler
{
    public static async Task Handle(
        PaymentFailed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var failed = new OrderFailed(evt.OrderId, $"Payment failed: {evt.Reason}");
        session.Events.Append(evt.OrderId, failed);
        await bus.PublishAsync(failed);
    }
}
