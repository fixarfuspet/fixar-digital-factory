using Fixar.Application.Common.Exceptions;
using Fixar.Application.Common.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace Fixar.API.Middleware;

/// <summary>
/// Central handler for every unhandled exception in the request pipeline.
/// Registered via <c>AddExceptionHandler</c> / <c>UseExceptionHandler</c>
/// in Program.cs. Maps known Application exceptions to the correct HTTP
/// status code and the standard <see cref="ApiResponse{T}"/> envelope;
/// anything unrecognised is logged and returned as a generic 500 so
/// internal details never leak to clients.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, errorCode, message) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", exception.Message),
            ValidationAppException => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", exception.Message),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, "FORBIDDEN", exception.Message),
            ConflictAppException conflict => (StatusCodes.Status409Conflict, conflict.ErrorCode, conflict.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Bu işlem için giriş yapmanız gerekiyor."),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR", "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}; correlation {CorrelationId}", httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception on {Method} {Path}: {Message}; correlation {CorrelationId}", httpContext.Request.Method, httpContext.Request.Path, exception.Message, httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message, errorCode);
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
