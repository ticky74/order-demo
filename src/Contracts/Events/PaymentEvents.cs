// src/Contracts/Events/PaymentEvents.cs
using Contracts.Models;

namespace Contracts.Events;

public record PaymentProcessed(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount);

// Includes Items so Inventory can issue compensating StockReleased
public record PaymentFailed(
    Guid PaymentId,
    Guid OrderId,
    string Reason,
    OrderItem[] Items);
