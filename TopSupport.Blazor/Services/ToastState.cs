namespace TopSupport.Blazor.Services;

public record Toast(Guid Id, string Message, bool IsError);

/// <summary>
/// Port of toast(msg, kind) / S.toasts in topsupport-app.js: a message shown bottom-right for
/// 4.2 seconds. ToastHost.razor (mounted in MainLayout) renders the list.
/// </summary>
public class ToastState
{
    // Show runs on the circuit, the timed removal on the thread pool: the list is locked.
    private readonly List<Toast> _toasts = new();

    /// <summary>A snapshot of the toasts showing now.</summary>
    public IReadOnlyList<Toast> Toasts
    {
        get
        {
            lock (_toasts) return _toasts.ToList();
        }
    }

    public event Action? Changed;

    public void Show(string message, bool isError = false)
    {
        var toast = new Toast(Guid.NewGuid(), message, isError);
        lock (_toasts) _toasts.Add(toast);
        Changed?.Invoke();

        _ = Task.Delay(4200).ContinueWith(_ =>
        {
            lock (_toasts) _toasts.Remove(toast);
            Changed?.Invoke();
        }, TaskScheduler.Default);
    }

    public void Error(string message) => Show(message, isError: true);
}
