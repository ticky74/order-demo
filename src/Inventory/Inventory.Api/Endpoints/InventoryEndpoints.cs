// src/Inventory/Inventory.Api/Endpoints/InventoryEndpoints.cs
namespace Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Health check — Aspire also adds /health via MapDefaultEndpoints
        app.MapGet("/", () => Results.Ok(new { service = "inventory-api", status = "running" }));
        return app;
    }
}
