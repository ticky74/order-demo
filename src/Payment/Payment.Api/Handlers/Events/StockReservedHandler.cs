// src/Payment/Payment.Api/Handlers/Events/StockReservedHandler.cs
using Contracts.Events;
using Marten;
using Payment.Api.Domain;
using Wolverine;

namespace Payment.Api.Handlers.Events;

public static class StockReservedHandler
{
    public static async Task Handle(
        StockReserved evt,
        IDocumentSession session,
        IMessageBus bus)
    {
        var paymentId = Guid.NewGuid();
        var total = evt.Items.Sum(i => i.UnitPrice * i.Quantity);

        // Simulate payment processing — always succeeds in this POC
        var processedEvt = new PaymentProcessed(paymentId, evt.OrderId, total);
        session.Events.StartStream<Payment.Api.Domain.Payment>(paymentId, processedEvt);
        await bus.PublishAsync(processedEvt);
        // Wolverine AutoApplyTransactions calls SaveChangesAsync here
    }
}
