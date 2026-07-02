namespace Fixar.Application.Common.Models;

/// <summary>
/// Standard response envelope used by every endpoint, matching the
/// contract defined in docs/09_API_ARCHITECTURE.md.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public string? ErrorCode { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, string? errorCode = null)
        => new() { Success = false, Message = message, ErrorCode = errorCode };
}
