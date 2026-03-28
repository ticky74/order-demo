// tests/Inventory.Api.Tests/Handlers/OrderPlacedHandlerTests.cs
using Contracts.Events;
using Contracts.Models;
using Inventory.Api.Domain;
using Inventory.Api.Handlers.Events;

namespace Inventory.Api.Tests.Handlers;

public class OrderPlacedHandlerTests
{
    [Fact]
    public void ReserveStock_ShouldReturnStockReserved_WhenSufficientStock()
    {
        // Arrange
        var itemId = Guid.NewGuid();
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(itemId, "Bauer", "Mask", "Hyperlite2", "", 899.99m, 5));

        var orderItems = new[] { new OrderItem(itemId, "Hyperlite2", "Bauer", 2, 899.99m) };
        var orderId = Guid.NewGuid();

        // Act
        var (success, stockReserved, stockFailed, updatedQty) =
            OrderPlacedHandlerLogic.TryReserve(orderId, orderItems, new[] { item });

        // Assert
        Assert.True(success);
        Assert.NotNull(stockReserved);
        Assert.Null(stockFailed);
        Assert.Equal(3, updatedQty[itemId]);  // 5 - 2
    }

    [Fact]
    public void ReserveStock_ShouldReturnFailed_WhenInsufficientStock()
    {
        var itemId = Guid.NewGuid();
        var item = new InventoryItem();
        item.Apply(new InventoryItemCreated(itemId, "Bauer", "Mask", "Hyperlite2", "", 899.99m, 1));

        var orderItems = new[] { new OrderItem(itemId, "Hyperlite2", "Bauer", 3, 899.99m) };

        var (success, stockReserved, stockFailed, _) =
            OrderPlacedHandlerLogic.TryReserve(Guid.NewGuid(), orderItems, new[] { item });

        Assert.False(success);
        Assert.Null(stockReserved);
        Assert.NotNull(stockFailed);
    }
}
