namespace OnlineStore.API.Dtos;

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal Subtotal);
