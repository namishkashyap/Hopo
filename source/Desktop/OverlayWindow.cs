#if ANDROID
using Avalonia.Controls;
using Avalonia.Media;

namespace Lantoss.Desktop;

/// <summary>
/// Android has no separate windows, so each dialog is an in-app panel over the page with the same content and look.
/// It offers the few window members the dialogs use; Back or a tap outside closes it like Cancel.
/// </summary>
internal sealed class OverlayWindow
{
    private Control? layer;
    private TaskCompletionSource<object?>? result;
    private bool opened;
    public string Title { get; set; } = "";
    public double Width { get; set; } = 560;
    public SizeToContent SizeToContent
    {
        get; set;
    }
    public double MaxHeight
    {
        get; set;
    }
    public bool CanResize
    {
        get; set;
    }
    public WindowStartupLocation WindowStartupLocation
    {
        get; set;
    }
    public IBrush? Background
    {
        get; set;
    }
    public Control? Content
    {
        get; set;
    }
    public bool IsVisible => layer != null;
    public event EventHandler? Opened, Closed;
    public void Activate()
    {
    }
    public void Close() => Close(null);
    public void Close(object? value)
    {
        if (layer == null)
            return;
        Shell.Current.HideOverlay(layer);
        layer = null;
        result?.TrySetResult(value);
        Closed?.Invoke(this, EventArgs.Empty);
    }
    public async Task<T> ShowDialog<T>(object? owner)
    {
        _ = owner;
        result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Control body = Content ?? new Panel();
        layer = Shell.Current.ShowOverlay(body, Close);
        if (!opened)
        {
            opened = true;
            Opened?.Invoke(this, EventArgs.Empty);
        }
        object? value = await result.Task;
        return value is T typed ? typed : default!;
    }
}
#endif
