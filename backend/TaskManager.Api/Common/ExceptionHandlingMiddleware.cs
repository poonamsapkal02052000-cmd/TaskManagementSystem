using Microsoft.AspNetCore.Mvc;

namespace TaskManager.Api.Common;

/// <summary>Converts exceptions into RFC 7807 ProblemDetails responses.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await WriteProblem(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            var detail = env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            await WriteProblem(context, StatusCodes.Status500InternalServerError, detail);
        }
    }

    private static Task WriteProblem(HttpContext context, int status, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrase(status),
            Detail = detail,
            Instance = context.Request.Path
        };
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private static string ReasonPhrase(int status) => status switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        _ => "Server Error"
    };
}
