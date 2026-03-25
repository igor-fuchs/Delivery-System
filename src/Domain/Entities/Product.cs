namespace DeliverySystem.Domain.Entities;

/// <summary>
/// Represents a product available for ordering in the delivery system.
/// </summary>
public sealed class Product
{
    /// <summary>Gets or sets the unique identifier of the product.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the product.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Gets or sets the description of the product.</summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether the product is available for ordering.
    /// <c>true</c> means in stock; <c>false</c> means blocked by the provider.
    /// </summary>
    public bool Stock { get; set; }

    /// <summary>Gets or sets the unit price of the product.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the UTC date and time when the product was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the collection of order items associated with this product.</summary>
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
