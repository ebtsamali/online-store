using System.ComponentModel.DataAnnotations;

namespace OnlineStore.API.Dtos;

public record LoginRequest(
    [Required, EmailAddress]
    string Email,

    [Required]
    string Password
);
