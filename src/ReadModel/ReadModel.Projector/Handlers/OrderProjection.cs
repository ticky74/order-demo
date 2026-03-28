// src/ReadModel/ReadModel.Projector/Handlers/OrderProjection.cs
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Data;
using ReadModel.Projector.Models;

namespace ReadModel.Projector.Handlers;

public static class OrderProjection
{
    public static async Task Handle(OrderPlaced evt, AppDbContext db)
    {
        var order = new OrderReadModel
        {
            Id = evt.OrderId,
            UserId = evt.UserId,
            Status = "Pending",
            TotalAmount = evt.Total,
            PlacedAt = DateTimeOffset.UtcNow,
        };

        foreach (var item in evt.Items)
        {
            order.Lines.Add(new OrderLineReadModel
            {
                Id = Guid.NewGuid(),
                OrderId = evt.OrderId,
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Brand = item.Brand,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
    }

    public static async Task Handle(OrderConfirmed evt, AppDbContext db)
    {
        await db.Orders
            .Where(o => o.Id == evt.OrderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, "Confirmed")
                .SetProperty(o => o.ConfirmedAt, evt.ConfirmedAt));
    }

    public static async Task Handle(OrderFailed evt, AppDbContext db)
    {
        await db.Orders
            .Where(o => o.Id == evt.OrderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, "Failed")
                .SetProperty(o => o.FailureReason, evt.Reason));
    }
}
