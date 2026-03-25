namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Represents a single product line item in a create-order request.
/// </summary>
/// <param name="ProductId">The identifier of the product to order.</param>
/// <param name="Quantity">The quantity to order. Must be greater than zero.</param>
public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
