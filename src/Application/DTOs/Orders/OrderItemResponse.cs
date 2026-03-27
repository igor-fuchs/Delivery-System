namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Represents a single line item within an order response.
/// </summary>
/// <param name="Id">The unique identifier of the order item.</param>
/// <param name="ProductId">The identifier of the product.</param>
/// <param name="ProductName">The name of the product at the time of the order.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The unit price of the product at the time the order was placed.</param>
public sealed record OrderItemResponse(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
