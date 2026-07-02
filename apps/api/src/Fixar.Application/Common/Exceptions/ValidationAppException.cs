namespace Fixar.Application.Common.Exceptions;

/// <summary>
/// Raised when request data fails validation. <see cref="Errors"/> maps
/// field name to the list of failure messages for that field, matching
/// the shape ASP.NET Core's ProblemDetails validation errors use.
/// </summary>
public class ValidationAppException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException()
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }
}
