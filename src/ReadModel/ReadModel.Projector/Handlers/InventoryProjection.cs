// src/ReadModel/ReadModel.Projector/Handlers/InventoryProjection.cs
using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using ReadModel.Projector.Data;
using ReadModel.Projector.Models;

namespace ReadModel.Projector.Handlers;

public class InventoryProjectionHandler
{
    public async Task Handle(InventoryItemCreated evt, AppDbContext db)
    {
        // Idempotent — skip if already exists
        if (await db.InventoryItems.AnyAsync(i => i.Id == evt.ItemId)) return;

        db.InventoryItems.Add(new InventoryItemReadModel
        {
            Id = evt.ItemId,
            Brand = evt.Brand,
            Category = evt.Category,
            Name = evt.Name,
            Description = evt.Description,
            Price = evt.Price,
            StockQty = evt.InitialStock,
        });
        await db.SaveChangesAsync();
    }

    public async Task Handle(StockQuantityUpdated evt, AppDbContext db)
    {
        await db.InventoryItems
            .Where(i => i.Id == evt.ItemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.StockQty, evt.NewQuantity));
    }
}
