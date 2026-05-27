namespace Backend.Exceptions;

public class ValidationException : Exception
{
    public ValidationException() { }

    public ValidationException(string message)
        : base(message) { }

    public ValidationException(string message, Exception inner)
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

public class ExternalServiceException : Exception
{
    public ExternalServiceException() { }

    public ExternalServiceException(string message)
        : base(message) { }

    public ExternalServiceException(string message, Exception inner)
        : base(message, inner) { }
}
