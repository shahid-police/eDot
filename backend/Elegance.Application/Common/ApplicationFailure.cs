namespace Elegance.Application.Common;

public enum FailureKind
{
    NotFound,
    Conflict,
    Unauthorized,
}

public class ApplicationFailure(FailureKind kind, string message) : Exception(message)
{
    public FailureKind Kind { get; } = kind;
}
