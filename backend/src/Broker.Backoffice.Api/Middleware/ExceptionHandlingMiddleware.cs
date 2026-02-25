using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Broker.Backoffice.Api.Middleware;

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
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed");
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred.",
                ex.Errors
                    .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray(),
                        StringComparer.OrdinalIgnoreCase));
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
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict");
            await WriteProblemDetails(context, StatusCodes.Status409Conflict,
                "Concurrency Conflict",
                "The record was modified by another user. Please refresh and try again.");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Database update failed");
            var detail = ex.InnerException?.Message?.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) == true
                ? "Cannot delete this record because it is referenced by other records."
                : "A database error occurred. Please try again.";
            await WriteProblemDetails(context, StatusCodes.Status409Conflict,
                "Conflict", detail);
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
        string detail,
        IDictionary<string, string[]>? errors = null)
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

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        await context.Response.WriteAsJsonAsync(problem);
    }
}
