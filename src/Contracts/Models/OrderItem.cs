// src/Contracts/Models/OrderItem.cs
namespace Contracts.Models;

public record OrderItem(
    Guid ItemId,
    string ItemName,
    string Brand,
    int Quantity,
    decimal UnitPrice);
