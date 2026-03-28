// src/Order/Order.Api/Domain/Order.cs
using Contracts.Events;
using Contracts.Models;

namespace Order.Api.Domain;

public enum OrderStatus { Pending, Confirmed, Failed }

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public OrderItem[] Items { get; private set; } = [];
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public void Apply(OrderPlaced evt)
    {
        Id = evt.OrderId;
        UserId = evt.UserId;
        Items = evt.Items;
        TotalAmount = evt.Total;
        Status = OrderStatus.Pending;
        PlacedAt = DateTimeOffset.UtcNow;
    }

    public void Apply(OrderConfirmed evt)
    {
        Status = OrderStatus.Confirmed;
        ConfirmedAt = evt.ConfirmedAt;
    }

    public void Apply(OrderFailed evt)
    {
        Status = OrderStatus.Failed;
        FailureReason = evt.Reason;
    }
}
