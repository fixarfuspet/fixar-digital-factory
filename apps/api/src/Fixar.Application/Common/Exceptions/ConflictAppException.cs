namespace Fixar.Application.Common.Exceptions;

public sealed class ConflictAppException(string message, string errorCode = "CONFLICT") : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
