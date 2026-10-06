namespace TaskManager.Api.Common;

/// <summary>Base type for errors that map to a specific HTTP status code.</summary>
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);

public class ForbiddenException(string message = "You do not have permission to perform this action.")
    : AppException(message, StatusCodes.Status403Forbidden);

public class BadRequestException(string message) : AppException(message, StatusCodes.Status400BadRequest);

public class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);

public class UnauthorizedException(string message) : AppException(message, StatusCodes.Status401Unauthorized);
