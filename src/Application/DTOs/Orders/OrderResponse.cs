namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Represents an order returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the order.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="Description">The description or notes for the order.</param>
/// <param name="Status">The current status of the order as a string.</param>
/// <param name="TotalAmount">The total monetary amount of the order.</param>
/// <param name="CreatedAt">The UTC date and time the order was created.</param>
/// <param name="UpdatedAt">The UTC date and time the order was last updated, or <c>null</c>.</param>
/// <param name="Items">The list of line items in the order.</param>
public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Description,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items);
