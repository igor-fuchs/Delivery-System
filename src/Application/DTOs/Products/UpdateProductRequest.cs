namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for updating an existing product.
/// </summary>
/// <param name="Name">The updated name of the product.</param>
/// <param name="Description">The updated description of the product.</param>
/// <param name="Stock">Whether the product is available for ordering.</param>
/// <param name="Price">The updated unit price of the product.</param>
public sealed record UpdateProductRequest(string Name, string Description, bool Stock, decimal Price);
