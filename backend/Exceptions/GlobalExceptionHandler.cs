using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Supabase.Gotrue.Exceptions;

namespace Backend.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, message) = exception switch
        {
            ValidationException ex => (400, ex.Message),
            GotrueException ex => (ex.StatusCode, ex.Message),
            NotFoundException ex => (404, ex.Message),
            ForbiddenException ex => (403, ex.Message),
            ExternalServiceException ex => (502, ex.Message),
            _ => (500, "An unexpected error occurred."),
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = message,
                Type = exception.GetType().Name,
            },
            cancellationToken
        );

        return true;
    }
}
