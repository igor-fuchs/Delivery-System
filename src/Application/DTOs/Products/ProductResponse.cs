namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Represents a product returned by the API.
/// </summary>
/// <param name="Id">The unique identifier of the product.</param>
/// <param name="Name">The name of the product.</param>
/// <param name="Description">The description of the product.</param>
/// <param name="Stock">Whether the product is available for ordering.</param>
/// <param name="Price">The unit price of the product.</param>
/// <param name="CreatedAt">The UTC date and time the product was created.</param>
public sealed record ProductResponse(Guid Id, string Name, string Description, bool Stock, decimal Price, DateTime CreatedAt);
