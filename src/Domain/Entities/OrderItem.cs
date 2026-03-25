namespace DeliverySystem.Domain.Entities;

/// <summary>
/// Represents a single line item within an order, linking a product to an order with quantity and price snapshot.
/// </summary>
public sealed class OrderItem
{
    /// <summary>Gets or sets the unique identifier of the order item.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the order this item belongs to.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the identifier of the product being ordered.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Gets or sets the quantity of the product ordered.</summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price of the product at the time the order was placed.
    /// Snapshotted so price changes do not retroactively affect existing orders.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the order this item belongs to.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>Gets or sets the product associated with this item.</summary>
    public Product Product { get; set; } = null!;
}
