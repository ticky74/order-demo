// src/Inventory/Inventory.Api/Domain/InventoryItem.cs
using Contracts.Events;

namespace Inventory.Api.Domain;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public string Brand { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    public void Apply(InventoryItemCreated evt)
    {
        Id = evt.ItemId;
        Brand = evt.Brand;
        Category = evt.Category;
        Name = evt.Name;
        Description = evt.Description;
        Price = evt.Price;
        StockQuantity = evt.InitialStock;
    }

    public void Apply(StockQuantityUpdated evt) =>
        StockQuantity = evt.NewQuantity;

    public bool HasSufficientStock(int required) =>
        StockQuantity >= required;
}
