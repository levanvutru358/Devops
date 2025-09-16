namespace EmoApi.Models;

public record CartItemDto(long Id, long ProductId, string ProductName, decimal Price, int Quantity);
public record CreateCartItemRequest(long ProductId, int Quantity);
public record UpdateCartItemRequest(int Quantity);
public record CreateOrderRequest(string ShippingName, string ShippingPhone, string ShippingAddress);
public record OrderDto(long Id, DateTime CreatedAt, decimal Total, string Status);
public record OrderItemDto(long ProductId, string ProductName, decimal Price, int Quantity);

