using DeliverySystem.Domain.Enums;

namespace DeliverySystem.Domain.Entities;

/// <summary>
/// Represents a customer order in the delivery system.
/// </summary>
public sealed class Order
{
    /// <summary>Gets or sets the unique identifier of the order.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the customer who placed the order.
    /// References <c>AspNetUsers.Id</c>; stored as a scalar to avoid a Domain → Infrastructure dependency.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets the description or notes for the order.</summary>
    public string Description { get; set; } = null!;

    /// <summary>Gets or sets the current status of the order.</summary>
    public OrderStatus Status { get; set; }

    /// <summary>Gets or sets the total monetary amount of the order at creation time.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the UTC date and time when the order was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC date and time when the order was last updated, or <c>null</c> if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Gets or sets the collection of line items included in this order.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
