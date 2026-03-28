// src/Order/Order.Api/Handlers/Events/PaymentProcessedHandler.cs
using Contracts.Events;
using Marten;
using Wolverine;

namespace Order.Api.Handlers.Events;

public static class PaymentProcessedHandler
{
    public static async Task Handle(
        PaymentProcessed evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var confirmed = new OrderConfirmed(evt.OrderId, DateTimeOffset.UtcNow);
        session.Events.Append(evt.OrderId, confirmed);
        await bus.PublishAsync(confirmed);
    }
}
