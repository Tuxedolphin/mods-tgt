using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.DTOs;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }
    public required long ExpiresIn { get; set; }
    public required string TokenType { get; set; }
}

public class SupabaseTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public required bool RequiresEmailConfirmation { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthResponse? Session { get; set; }
}

public class MeResponse
{
    public string? Username { get; set; }
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
