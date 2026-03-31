using DeliverySystem.Application.DTOs;

namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Abstraction for order management operations (CRUD).
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Returns orders. Pass <c>null</c> for <paramref name="customerId"/> to return all (admin).
    /// </summary>
    /// <param name="customerId">Filter by customer, or <c>null</c> for all orders.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of matching orders.</returns>
    Task<IReadOnlyList<OrderResponse>> GetAllAsync(Guid? customerId, CancellationToken ct = default);

    /// <summary>
    /// Returns a single order by its identifier.
    /// When <paramref name="requesterId"/> is provided, verifies ownership.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="requesterId">
    /// The ID of the requesting user for ownership verification, or <c>null</c> to skip the check (admin).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="OrderResponse"/>.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the order does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the requester does not own the order.</exception>
    Task<OrderResponse> GetByIdAsync(Guid id, Guid? requesterId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a new order for the specified customer.
    /// </summary>
    /// <param name="customerId">The identifier of the customer placing the order.</param>
    /// <param name="request">The order creation data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created <see cref="OrderResponse"/>.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when a referenced product does not exist.</exception>
    /// <exception cref="Exceptions.ValidationException">Thrown when a referenced product is unavailable.</exception>
    Task<OrderResponse> CreateAsync(Guid customerId, CreateOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an existing order.
    /// </summary>
    /// <param name="id">The identifier of the order to update.</param>
    /// <param name="request">The new status data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="OrderResponse"/>.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the order does not exist.</exception>
    Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an order by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the order to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the order does not exist.</exception>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
