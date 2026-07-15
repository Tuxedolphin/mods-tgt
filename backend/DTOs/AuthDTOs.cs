using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; init; } = string.Empty;
}

public record RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public record AuthResponse
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }
    public required long ExpiresIn { get; init; }
    public required string TokenType { get; init; }
}

public record SupabaseTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;
}

public record RegisterResponse
{
    public required bool RequiresEmailConfirmation { get; init; }
    public string Message { get; init; } = string.Empty;
    public AuthResponse? Session { get; init; }
}

public record MeResponse
{
    public string? Username { get; init; }
}

public record ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}

public record ResetPasswordRequest
{
    [Required]
    public required string TokenHash { get; init; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public required string Password { get; init; }
}

public record UpdatePasswordRequest
{
    [Required]
    public required string OldPassword { get; init; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public required string NewPassword { get; init; }
}
