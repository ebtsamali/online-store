namespace OnlineStore.API.Dtos;

public record AuthResponse(string Token, string Name, string Email, string Role);
