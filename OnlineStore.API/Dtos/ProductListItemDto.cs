namespace OnlineStore.API.Dtos;

public record ProductListItemDto(int Id, string Name, decimal Price, int Stock, bool IsActive, string ImageUrl);
