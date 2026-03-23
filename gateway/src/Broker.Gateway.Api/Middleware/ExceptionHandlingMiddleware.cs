using Microsoft.AspNetCore.Mvc;

namespace Broker.Gateway.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized");
            await WriteProblemDetails(context, StatusCodes.Status401Unauthorized,
                "Unauthorized", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Not found");
            await WriteProblemDetails(context, StatusCodes.Status404NotFound,
                "Not Found", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Conflict");
            await WriteProblemDetails(context, StatusCodes.Status409Conflict,
                "Conflict", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemDetails(context, StatusCodes.Status500InternalServerError,
                "Server Error",
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
