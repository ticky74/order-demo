// src/Order/Order.Api/Handlers/Commands/PlaceOrderHandler.cs
using Contracts.Events;
using Marten;
using Order.Api.Commands;
using Order.Api.Domain;
using Wolverine;

namespace Order.Api.Handlers.Commands;

public static class PlaceOrderHandler
{
    public static async Task<PlaceOrderResult> Handle(
        PlaceOrderCommand cmd,
        IDocumentSession session,
        IMessageBus bus)
    {
        var evt = new OrderPlaced(cmd.OrderId, cmd.UserId, cmd.Items, cmd.Total);
        session.Events.StartStream<Domain.Order>(cmd.OrderId, evt);
        await bus.PublishAsync(evt);
        // Wolverine AutoApplyTransactions calls SaveChangesAsync here
        return new PlaceOrderResult(cmd.OrderId);
    }
}
