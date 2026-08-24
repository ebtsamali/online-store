using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record CategoryRequest(
    [Required, StringLength(100, MinimumLength = 1)]
    string Name
);
