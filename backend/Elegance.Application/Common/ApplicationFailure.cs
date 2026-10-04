namespace Elegance.Application.Common;

public enum FailureKind
{
    NotFound,
    Conflict,
    Unauthorized,
    Validation,
}

public class ApplicationFailure(FailureKind kind, string message) : Exception(message)
{
    public FailureKind Kind { get; } = kind;
}
