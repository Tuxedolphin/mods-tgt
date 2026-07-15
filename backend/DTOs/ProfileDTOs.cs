using System.ComponentModel.DataAnnotations;
using Backend.Services.Profiles;

namespace Backend.DTOs;

public record ProfileResponse(Guid UserId, string? Username, string? Handle, string? AvatarUrl);

public record HandleAvailabilityResponse(bool Available, HandleUnavailableReason? Reason);

public record UpdateProfileRequest
{
    [Required]
    public required Guid UserId { get; init; }

    [Required]
    [MinLength(
        ProfileRules.MinLength,
        ErrorMessage = "Username must be minimally {1} characters in length."
    )]
    [MaxLength(
        ProfileRules.MaxLength,
        ErrorMessage = "Username must be at most {1} characters in length."
    )]
    [RegularExpression(
        ProfileRules.BasicUsernamePattern,
        ErrorMessage = "Username can only contain characters, letters, spaces, dashes, and underscores."
    )]
    public required string Username { get; init; }

    [Required]
    public required string Handle { get; init; }
}

public record UpsertAvatarRequest(IFormFile File);

public enum HandleUnavailableReason
{
    Taken,
    Reserved,
    InvalidFormat,
    TooShort,
    TooLong,
}
