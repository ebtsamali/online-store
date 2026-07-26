namespace OnlineStore.API.Dtos;

public record OrderDetailDto(
    int Id,
    string Status,
    decimal Total,
    DateTime CreatedAt,
    List<OrderItemDto> Items);
