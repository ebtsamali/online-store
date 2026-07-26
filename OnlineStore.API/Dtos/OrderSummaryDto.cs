namespace OnlineStore.API.Dtos;

public record OrderSummaryDto(
    int Id,
    string Status,
    decimal Total,
    DateTime CreatedAt);
