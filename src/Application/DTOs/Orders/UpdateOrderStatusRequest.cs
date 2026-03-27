namespace DeliverySystem.Application.DTOs;

/// <summary>
/// Request payload for updating the status of an existing order.
/// </summary>
/// <param name="Status">
/// The new order status as a string (e.g. <c>"Processing"</c>, <c>"Shipped"</c>).
/// Must match a valid order status value (e.g. Pending, Processing, Shipped, Delivered, Cancelled).
/// </param>
public sealed record UpdateOrderStatusRequest(string Status);
