// tests/Order.Api.Tests/Domain/OrderTests.cs
using Contracts.Events;
using Contracts.Models;
using Order.Api.Domain;

namespace Order.Api.Tests.Domain;

public class OrderTests
{
    private static OrderPlaced MakeOrderPlaced(Guid? orderId = null) =>
        new(orderId ?? Guid.NewGuid(), Guid.NewGuid(),
            [new OrderItem(Guid.NewGuid(), "Bauer Hyperlite2", "Bauer", 1, 899.99m)],
            899.99m);

    [Fact]
    public void Apply_OrderPlaced_SetsPendingStatus()
    {
        var order = new Order.Api.Domain.Order();
        order.Apply(MakeOrderPlaced());
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Apply_OrderConfirmed_SetsConfirmedStatus()
    {
        var order = new Order.Api.Domain.Order();
        var placed = MakeOrderPlaced();
        order.Apply(placed);
        order.Apply(new OrderConfirmed(placed.OrderId, DateTimeOffset.UtcNow));
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Apply_OrderFailed_SetsFailedStatus()
    {
        var order = new Order.Api.Domain.Order();
        var placed = MakeOrderPlaced();
        order.Apply(placed);
        order.Apply(new OrderFailed(placed.OrderId, "Out of stock"));
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal("Out of stock", order.FailureReason);
    }
}
