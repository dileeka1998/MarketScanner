namespace MarketScanner.Services;

/// <summary>
/// Service for thread-safe UI operations.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Executes an action on the UI thread synchronously.
    /// </summary>
    void OnUI(Action action);
    
    /// <summary>
    /// Executes an action on the UI thread asynchronously.
    /// </summary>
    Task OnUIAsync(Action action);
    
    /// <summary>
    /// Executes a function on the UI thread asynchronously.
    /// </summary>
    Task OnUIAsync(Func<Task> action);
    
    /// <summary>
    /// Executes a function on the UI thread and returns the result.
    /// </summary>
    Task<T> OnUIAsync<T>(Func<T> func);
    
    /// <summary>
    /// Executes a function on the UI thread asynchronously (alias for OnUIAsync).
    /// </summary>
    Task InvokeAsync(Func<Task> action);
}
