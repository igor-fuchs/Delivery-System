namespace DeliverySystem.Domain.Enums;

/// <summary>
/// Represents the lifecycle state of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order has been placed but not yet processed.</summary>
    Pending,

    /// <summary>Order is being prepared or picked.</summary>
    Processing,

    /// <summary>Order has been dispatched for delivery.</summary>
    Shipped,

    /// <summary>Order has been successfully delivered to the customer.</summary>
    Delivered,

    /// <summary>Order was cancelled before or during processing.</summary>
    Cancelled
}
