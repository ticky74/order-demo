// tests/Inventory.Api.Tests/Domain/InventoryItemTests.cs
using Contracts.Events;
using Inventory.Api.Domain;

namespace Inventory.Api.Tests.Domain;

public class InventoryItemTests
{
    [Fact]
    public void Apply_InventoryItemCreated_SetsAllProperties()
    {
        var item = new InventoryItem();
        var itemId = Guid.NewGuid();
        item.Apply(new InventoryItemCreated(
            itemId, "Bauer", "Mask", "Bauer Hyperlite2", "Top mask", 899.99m, 8));

        Assert.Equal(itemId, item.Id);
        Assert.Equal("Bauer", item.Brand);
        Assert.Equal("Mask", item.Category);
        Assert.Equal("Bauer Hyperlite2", item.Name);
        Assert.Equal(899.99m, item.Price);
        Assert.Equal(8, item.StockQuantity);
    }

    [Fact]
    public void Apply_StockQuantityUpdated_UpdatesStock()
    {
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Mask", "Hyperlite2", "", 899.99m, 8));
        item.Apply(new StockQuantityUpdated(item.Id, 5));

        Assert.Equal(5, item.StockQuantity);
    }

    [Fact]
    public void HasSufficientStock_ReturnsTrueWhenEnough()
    {
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(Guid.NewGuid(), "Bauer", "Mask", "Hyperlite2", "", 899.99m, 8));

        Assert.True(item.HasSufficientStock(8));
        Assert.False(item.HasSufficientStock(9));
    }
}
