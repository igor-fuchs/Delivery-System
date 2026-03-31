using DeliverySystem.Application.DTOs;

namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for product management operations (CRUD).
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Returns all products ordered by name.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all products.</returns>
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a single product by its identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="ProductResponse"/>.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when no product with the given <paramref name="id"/> exists.</exception>
    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">The product creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created <see cref="ProductResponse"/>.</returns>
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The identifier of the product to update.</param>
    /// <param name="request">The updated product data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="ProductResponse"/>.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when no product with the given <paramref name="id"/> exists.</exception>
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a product by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the product to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="Exceptions.NotFoundException">Thrown when no product with the given <paramref name="id"/> exists.</exception>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
