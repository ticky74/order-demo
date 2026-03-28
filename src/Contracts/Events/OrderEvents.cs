// src/Contracts/Events/OrderEvents.cs
using Contracts.Models;

namespace Contracts.Events;

public record OrderPlaced(
    Guid OrderId,
    Guid UserId,
    OrderItem[] Items,
    decimal Total);

public record OrderConfirmed(
    Guid OrderId,
    DateTimeOffset ConfirmedAt);

public record OrderFailed(
    Guid OrderId,
    string Reason);
