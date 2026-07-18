using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record AddToCartRequest(
    [Range(1, int.MaxValue, ErrorMessage = "ProductId is required.")]
    int ProductId,

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    int Quantity
);
