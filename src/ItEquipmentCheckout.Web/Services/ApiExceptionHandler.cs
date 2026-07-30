using ItEquipmentCheckout.Core.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ItEquipmentCheckout.Web.Services;

public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int status, string title, string detail, string? code) = exception switch
        {
            ResourceNotFoundException notFound =>
                (StatusCodes.Status404NotFound, "Resource not found", notFound.Message, "not_found"),
            BusinessRuleException businessRule =>
                (StatusCodes.Status409Conflict, "Business rule conflict", businessRule.Message, businessRule.Code),
            DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 19 } } =>
                (StatusCodes.Status409Conflict, "Database constraint conflict",
                    "A unique or data integrity constraint was violated.", "constraint_conflict"),
            _ =>
                (StatusCodes.Status500InternalServerError, "Unexpected server error",
                    "An unexpected error occurred.", null),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledRequest(logger, exception);
        }
        else
        {
            LogRejectedRequest(logger, status, exception);
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }

    [LoggerMessage(1, LogLevel.Error, "Unhandled request exception")]
    private static partial void LogUnhandledRequest(ILogger logger, Exception exception);

    [LoggerMessage(2, LogLevel.Information, "Request rejected with status {StatusCode}")]
    private static partial void LogRejectedRequest(
        ILogger logger,
        int statusCode,
        Exception exception);
}
