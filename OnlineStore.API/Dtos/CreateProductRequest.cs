using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record CreateProductRequest(
    [Required, StringLength(100)]
    string Name,

    string Description,

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater.")]
    int Stock,

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    int CategoryId,

    [Range(1, int.MaxValue, ErrorMessage = "BrandId is required.")]
    int BrandId
);
