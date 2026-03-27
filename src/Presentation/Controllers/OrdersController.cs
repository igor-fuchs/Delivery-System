using System.Security.Claims;
using DeliverySystem.Application.Constants;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Exceptions;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Domain.Constants;
using DeliverySystem.Presentation.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DeliverySystem.Presentation.Controllers;

/// <summary>
/// API controller for order management (CRUD).
/// Authenticated users can create and view their own orders; admins can manage all orders.
/// Order creation requires an <c>Idempotency-Key</c> header to prevent duplicate orders.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Orders")]
[Produces("application/json")]
[Authorize(Policy = AppRoles.DefaultPolicy)]
[EnableRateLimiting(RateLimitOptions.OrdersPolicyName)]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrdersController"/> class.
    /// </summary>
    /// <param name="orderService">The order service.</param>
    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Returns orders. Admins receive all orders; regular users receive only their own.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of orders.</returns>
    /// <response code="200">Orders retrieved successfully.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        Guid? customerId = User.IsInRole(AppRoles.Admin) ? null : GetCurrentUserId();
        var orders = await _orderService.GetAllAsync(customerId, ct);
        return Ok(orders);
    }

    /// <summary>
    /// Returns a single order by its identifier.
    /// Regular users may only retrieve their own orders.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching order.</returns>
    /// <response code="200">Order retrieved successfully.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Order belongs to a different customer.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        Guid? requesterId = User.IsInRole(AppRoles.Admin) ? null : GetCurrentUserId();
        var order = await _orderService.GetByIdAsync(id, requesterId, ct);
        return Ok(order);
    }

    /// <summary>
    /// Creates a new order for the authenticated user.
    /// </summary>
    /// <param name="request">The order creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created order.</returns>
    /// <response code="201">Order created successfully.</response>
    /// <response code="400">Validation errors, unavailable product, or missing/invalid Idempotency-Key header.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">A referenced product was not found.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPost]
    [IdempotencyFilter]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var order = await _orderService.CreateAsync(GetCurrentUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Updates the status of an existing order. Requires the admin role.
    /// </summary>
    /// <param name="id">The identifier of the order to update.</param>
    /// <param name="request">The new status data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated order.</returns>
    /// <response code="200">Order status updated successfully.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var order = await _orderService.UpdateStatusAsync(id, request, ct);
        return Ok(order);
    }

    /// <summary>
    /// Deletes an order by its identifier. Requires the admin role.
    /// </summary>
    /// <param name="id">The identifier of the order to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Order deleted successfully.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _orderService.DeleteAsync(id, ct);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new AppUnauthorizedException("User identity could not be determined.", ErrorCodes.UserIdentityMissing);
        return Guid.Parse(value);
    }
}
