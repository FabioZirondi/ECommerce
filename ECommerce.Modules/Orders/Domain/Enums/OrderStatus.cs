namespace ECommerce.Modules.Orders.Domain.Entities;

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
