using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Supabase.Gotrue.Exceptions;

namespace Backend.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        var (statusCode, message, errorCode) = exception switch
        {
            BadRequestException ex => (400, ex.Message, (string?)null),
            GotrueException ex => MapSupabaseError(
                ex.StatusCode,
                ex.Content ?? ex.Message,
                ex.Reason
            ),
            SupabaseAuthException ex => MapSupabaseError(
                ex.StatusCode,
                ex.Content,
                FailureHint.Reason.Unknown
            ),
            UnauthorizedAccessException ex => (401, ex.Message, null),
            NotFoundException ex => (404, ex.Message, null),
            ForbiddenException ex => (403, ex.Message, null),
            ExternalServiceException ex => (502, ex.Message, null),
            _ => (500, "An unexpected error occurred.", null),
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = message,
            Type = exception.GetType().Name,
        };

        if (errorCode != null)
        {
            problemDetails.Extensions["errorCode"] = errorCode;
        }

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    // Supabase returns specific errors which aren't very friendly when we return it to the backend
    private static (int, string, string?) MapSupabaseError(
        int statusCode,
        string? content,
        FailureHint.Reason reason
    )
    {
        var errorCode = TryGetErrorCode(content);

        // Manual mapping
        var byErrorCode = errorCode switch
        {
            "invalid_credentials" => (401, "Invalid email or password."),
            "email_not_confirmed" => (403, "Please confirm your email address first."),
            "user_already_exists" or "email_exists" or "phone_exists" => (
                409,
                "An account with this email already exists."
            ),
            "over_request_rate_limit" or "over_email_send_rate_limit" => (
                429,
                "Too many attempts. Please try again later."
            ),
            "refresh_token_not_found"
            or "refresh_token_already_used"
            or "session_not_found"
            or "session_expired"
            or "bad_jwt" => (401, "Session expired. Please log in again."),
            "otp_expired" => (401, "This link has expired. Please request a new one."),
            "weak_password" => (400, "Password does not meet the strength requirements."),
            "same_password" => (400, "New password must be different from the current password."),
            "validation_failed" => (400, "Invalid request."),
            _ => default((int, string)?),
        };

        if (byErrorCode is { } mapped)
            return (mapped.Item1, mapped.Item2, errorCode);

        return reason switch
        {
            FailureHint.Reason.UserBadLogin
            or FailureHint.Reason.UserBadPassword
            or FailureHint.Reason.UserBadMultiple => (401, "Invalid email or password.", errorCode),
            FailureHint.Reason.UserEmailNotConfirmed => (
                403,
                "Please confirm your email address first.",
                errorCode
            ),
            FailureHint.Reason.UserAlreadyRegistered => (
                409,
                "An account with this email already exists.",
                errorCode
            ),
            FailureHint.Reason.UserTooManyRequests => (
                429,
                "Too many attempts. Please try again later.",
                errorCode
            ),
            FailureHint.Reason.InvalidRefreshToken
            or FailureHint.Reason.ExpiredRefreshToken
            or FailureHint.Reason.NoSessionFound => (
                401,
                "Session expired. Please log in again.",
                errorCode
            ),
            FailureHint.Reason.UserBadEmailAddress => (400, "Invalid email address.", errorCode),
            FailureHint.Reason.UserMissingInformation => (
                400,
                "Missing required information.",
                errorCode
            ),
            FailureHint.Reason.Offline => (
                503,
                "Authentication service is unreachable.",
                errorCode
            ),
            _ => (
                statusCode is >= 400 and < 500 ? statusCode : 502,
                "Authentication service error.",
                errorCode
            ),
        };
    }

    private static string? TryGetErrorCode(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("error_code", out var code)
                ? code.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
