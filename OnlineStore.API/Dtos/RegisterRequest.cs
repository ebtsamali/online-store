using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record RegisterRequest(
    [Required, StringLength(50, MinimumLength = 3)]
    string Name,

    [Required, EmailAddress]
    string Email,

    [Required, MinLength(8)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter and one number.")]
    string Password
);
