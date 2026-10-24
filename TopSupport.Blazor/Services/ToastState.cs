namespace TopSupport.Blazor.Services;

public record Toast(Guid Id, string Message, bool IsError);

/// <summary>
/// Port of toast(msg, kind) / S.toasts in topsupport-app.js: a message shown bottom-right for
/// 4.2 seconds. ToastHost.razor (mounted in MainLayout) renders the list.
/// </summary>
public class ToastState
{
    private readonly List<Toast> _toasts = new();

    public IReadOnlyList<Toast> Toasts => _toasts;

    public event Action? Changed;

    public void Show(string message, bool isError = false)
    {
        var toast = new Toast(Guid.NewGuid(), message, isError);
        _toasts.Add(toast);
        Changed?.Invoke();

        _ = Task.Delay(4200).ContinueWith(_ =>
        {
            _toasts.Remove(toast);
            Changed?.Invoke();
        }, TaskScheduler.Default);
    }

    public void Error(string message) => Show(message, isError: true);
}
