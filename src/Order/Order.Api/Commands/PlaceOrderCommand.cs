// src/Order/Order.Api/Commands/PlaceOrderCommand.cs
using Contracts.Models;

namespace Order.Api.Commands;

public record PlaceOrderCommand(Guid OrderId, Guid UserId, OrderItem[] Items, decimal Total);
public record PlaceOrderResult(Guid OrderId);
public record PlaceOrderRequest(Guid UserId, OrderItem[] Items);
