namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for creating a new order.
/// </summary>
/// <param name="Description">A description or notes for the order.</param>
/// <param name="Items">The list of products and quantities to include in the order.</param>
public sealed record CreateOrderRequest(string Description, IReadOnlyList<CreateOrderItemRequest> Items);
