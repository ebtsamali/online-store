using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OnlineStore.API.Dtos;

public class UpdateProductFormRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater.")]
    public int Stock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId is required.")]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BrandId is required.")]
    public int BrandId { get; set; }

    public bool IsActive { get; set; }

    public IFormFile? Image { get; set; }
}
