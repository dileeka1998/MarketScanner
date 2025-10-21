using Microsoft.Maui.Controls;

namespace MarketScanner.Services;

/// <summary>
/// MAUI implementation of the dispatcher service.
/// </summary>
public sealed class MauiDispatcherService : IDispatcherService
{
    public void OnUI(Action action)
    {
        if (MainThread.IsMainThread)
        {
            action();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }

    public async Task OnUIAsync(Action action)
    {
        if (MainThread.IsMainThread)
        {
            action();
            return;
        }
        
        try
        {
            var tcs = new TaskCompletionSource();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try 
                { 
                    action(); 
                    tcs.SetResult(); 
                }
                catch (Exception ex) 
                { 
                    tcs.SetException(ex); 
                }
            });
            await tcs.Task;
        }
        catch (InvalidOperationException)
        {
            // Main thread not available - execute on current thread as fallback
            action();
        }
    }

    public async Task OnUIAsync(Func<Task> action)
    {
        if (MainThread.IsMainThread)
        {
            await action();
        }
        else
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(action);
            }
            catch (InvalidOperationException)
            {
                // Main thread not available - execute on current thread as fallback
                await action();
            }
        }
    }

    public async Task<T> OnUIAsync<T>(Func<T> func)
    {
        if (MainThread.IsMainThread)
        {
            return func();
        }
        else
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(func);
            }
            catch (InvalidOperationException)
            {
                // Main thread not available - execute on current thread as fallback
                return func();
            }
        }
    }

    public async Task InvokeAsync(Func<Task> action)
    {
        await OnUIAsync(action);
    }
}
