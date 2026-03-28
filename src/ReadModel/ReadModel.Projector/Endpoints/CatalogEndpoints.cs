// src/ReadModel/ReadModel.Projector/Endpoints/CatalogEndpoints.cs
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Data;

namespace ReadModel.Projector.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new { service = "readmodel-projector", status = "running" }));

        // GET /api/catalog/items — all inventory items
        app.MapGet("/api/catalog/items", async (AppDbContext db) =>
        {
            var items = await db.InventoryItems
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Brand)
                .Select(i => new
                {
                    i.Id,
                    i.Brand,
                    i.Category,
                    i.Name,
                    i.Description,
                    i.Price,
                    i.StockQty,
                })
                .ToListAsync();
            return Results.Ok(items);
        });

        // GET /api/catalog/orders?userId={userId} — orders for a user
        app.MapGet("/api/catalog/orders", async (Guid userId, AppDbContext db) =>
        {
            var orders = await db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.PlacedAt)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.PlacedAt,
                    o.ConfirmedAt,
                    o.FailureReason,
                })
                .ToListAsync();
            return Results.Ok(orders);
        });

        // GET /api/catalog/orders/{orderId} — single order with lines
        app.MapGet("/api/catalog/orders/{orderId:guid}", async (Guid orderId, AppDbContext db) =>
        {
            var order = await db.Orders
                .Include(o => o.Lines)
                .Where(o => o.Id == orderId)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.Status,
                    o.TotalAmount,
                    o.PlacedAt,
                    o.ConfirmedAt,
                    o.FailureReason,
                    Lines = o.Lines.Select(l => new
                    {
                        l.Id,
                        l.ItemId,
                        l.ItemName,
                        l.Brand,
                        l.Quantity,
                        l.UnitPrice,
                    }),
                })
                .FirstOrDefaultAsync();

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        return app;
    }
}
