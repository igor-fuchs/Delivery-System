namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for creating a new product.
/// </summary>
/// <param name="Name">The name of the product.</param>
/// <param name="Description">The description of the product.</param>
/// <param name="Stock">Whether the product is available for ordering.</param>
/// <param name="Price">The unit price of the product.</param>
public sealed record CreateProductRequest(string Name, string Description, bool Stock, decimal Price);
