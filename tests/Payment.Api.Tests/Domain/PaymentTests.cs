// tests/Payment.Api.Tests/Domain/PaymentTests.cs
using Contracts.Events;
using Contracts.Models;
using Payment.Api.Domain;

namespace Payment.Api.Tests.Domain;

public class PaymentTests
{
    [Fact]
    public void Apply_PaymentProcessed_SetsProcessedStatus()
    {
        var payment = new Payment.Api.Domain.Payment();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        payment.Apply(new PaymentProcessed(paymentId, orderId, 899.99m));
        Assert.Equal(PaymentStatus.Processed, payment.Status);
        Assert.Equal(899.99m, payment.Amount);
    }

    [Fact]
    public void Apply_PaymentFailed_SetsFailedStatus()
    {
        var payment = new Payment.Api.Domain.Payment();
        var items = new[] { new OrderItem(Guid.NewGuid(), "Bauer Mask", "Bauer", 1, 899.99m) };
        payment.Apply(new PaymentFailed(Guid.NewGuid(), Guid.NewGuid(), "Card declined", items));
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Card declined", payment.FailureReason);
    }
}
