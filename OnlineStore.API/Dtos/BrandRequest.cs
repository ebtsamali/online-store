using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record BrandRequest(
    [Required, StringLength(100, MinimumLength = 1)]
    string Name
);
