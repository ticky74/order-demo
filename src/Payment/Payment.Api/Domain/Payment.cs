// src/Payment/Payment.Api/Domain/Payment.cs
using Contracts.Events;

namespace Payment.Api.Domain;

public enum PaymentStatus { Pending, Processed, Failed }

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public void Apply(PaymentProcessed evt)
    {
        Id = evt.PaymentId;
        OrderId = evt.OrderId;
        Amount = evt.Amount;
        Status = PaymentStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void Apply(PaymentFailed evt)
    {
        Id = evt.PaymentId;
        OrderId = evt.OrderId;
        Status = PaymentStatus.Failed;
        FailureReason = evt.Reason;
    }
}
