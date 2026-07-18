namespace OnlineStore.API.Dtos;

public record CartResponse(List<CartItemDto> Items, decimal Total);
