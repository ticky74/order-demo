// src/Contracts/Events/InventoryEvents.cs
using Contracts.Models;

namespace Contracts.Events;

public record InventoryItemCreated(
    Guid ItemId,
    string Brand,
    string Category,
    string Name,
    string Description,
    decimal Price,
    int InitialStock);

// StockReserved includes Items so compensating handlers know quantities
public record StockReserved(
    Guid OrderId,
    OrderItem[] Items);

public record StockReservationFailed(
    Guid OrderId,
    string Reason);

// Includes Items so Inventory knows how much stock to restore
public record StockReleased(
    Guid OrderId,
    OrderItem[] Items);

public record StockQuantityUpdated(
    Guid ItemId,
    int NewQuantity);
