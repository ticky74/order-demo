// src/Inventory/Inventory.Api/Endpoints/InventoryEndpoints.cs
using Contracts.Events;
using Marten;

namespace Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Health check — Aspire also adds /health via MapDefaultEndpoints
        app.MapGet("/", () => Results.Ok(new { service = "inventory-api", status = "running" }));

        // GET /api/catalog/items — serve catalog directly from the event store
        app.MapGet("/api/catalog/items", async (IQuerySession session) =>
        {
            var events = await session.Events
                .QueryRawEventDataOnly<InventoryItemCreated>()
                .ToListAsync();

            return Results.Ok(events
                .OrderBy(e => e.Category)
                .ThenBy(e => e.Brand)
                .Select(e => new
                {
                    Id = e.ItemId,
                    e.Brand,
                    e.Category,
                    e.Name,
                    e.Description,
                    e.Price,
                    StockQty = e.InitialStock,
                }));
        });

        return app;
    }
}
