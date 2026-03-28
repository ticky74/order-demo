// src/Order/Order.Api/Endpoints/OrderEndpoints.cs
using Order.Api.Commands;
using Wolverine;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new { service = "order-api", status = "running" }));

        app.MapPost("/api/orders", async (PlaceOrderRequest req, IMessageBus bus) =>
        {
            if (req.Items is null || req.Items.Length == 0)
                return Results.BadRequest("Order must contain at least one item.");

            var total = req.Items.Sum(i => i.UnitPrice * i.Quantity);
            var cmd = new PlaceOrderCommand(Guid.NewGuid(), req.UserId, req.Items, total);
            var result = await bus.InvokeAsync<PlaceOrderResult>(cmd);
            return Results.Accepted($"/api/orders/{result.OrderId}", result);
        });

        return app;
    }
}
