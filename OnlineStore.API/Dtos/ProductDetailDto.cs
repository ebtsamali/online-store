namespace OnlineStore.API.Dtos;

public record ProductDetailDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId,
    int BrandId,
    bool IsActive,
    DateTime CreatedAt);
