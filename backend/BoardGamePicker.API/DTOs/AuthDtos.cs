using System.ComponentModel.DataAnnotations;

namespace BoardGamePicker.API.DTOs;

public record RegisterDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password
);

public record LoginDto(
    [Required] string Email,
    [Required] string Password
);

public record AuthResponseDto(string Token, string Email, string Role);
