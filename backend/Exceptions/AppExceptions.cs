namespace Backend.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException() { }

    public BadRequestException(string message)
        : base(message) { }

    public BadRequestException(string message, Exception inner)
        : base(message, inner) { }
}

public class NotFoundException : Exception
{
    public NotFoundException() { }

    public NotFoundException(string message)
        : base(message) { }

    public NotFoundException(string message, Exception inner)
        : base(message, inner) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException() { }

    public ForbiddenException(string message)
        : base(message) { }

    public ForbiddenException(string message, Exception inner)
        : base(message, inner) { }
}

public class ConflictException : Exception
{
    public ConflictException() { }

    public ConflictException(string message)
        : base(message) { }

    public ConflictException(string message, Exception inner)
        : base(message, inner) { }
}

public class SupabaseAuthException : Exception
{
    public SupabaseAuthException() { }

    public SupabaseAuthException(string? message)
        : base(message) { }

    public SupabaseAuthException(string? message, Exception? innerException)
        : base(message, innerException) { }

    public SupabaseAuthException(int statusCode, string content)
        : base(content)
    {
        StatusCode = statusCode;
        Content = content;
    }

    public int StatusCode { get; }

    public string? Content { get; }
}

public class ExternalServiceException : Exception
{
    public ExternalServiceException() { }

    public ExternalServiceException(string message)
        : base(message) { }

    public ExternalServiceException(string message, Exception inner)
        : base(message, inner) { }
}
