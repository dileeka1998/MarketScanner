namespace MarketScanner.Models;

/// <summary>
/// Represents the result of a request operation.
/// </summary>
public sealed record RequestResult(
    RequestResultKind Kind,
    string? Reason = null,
    Exception? Exception = null
);

/// <summary>
/// Represents the kind of request result.
/// </summary>
public enum RequestResultKind
{
    Ok,
    Error,
    Timeout,
    Cancelled,
    NotFound,
    Unauthorized,
    Forbidden,
    RateLimited
}
