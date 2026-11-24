using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Lantoss.Desktop;

namespace Lantoss.Android;

/// <summary>The same hopo screens as on the PC (the shared shell from source/Desktop), on the phone's screen.</summary>
public sealed class App : Avalonia.Application
{
    private static Shell? shell;
    public override void Initialize()
    {
        Name = "hopo";
        Styles.Add(new FluentTheme());
    }
    public override void OnFrameworkInitializationCompleted()
    {
        // An error inside the screen is recorded and shown instead of closing the app.
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            CrashReport.Save(e.Exception, "Screen");
            e.Handled = true;
            shell?.Tell("Something went wrong: " + e.Exception.Message);
        };
        if (HopoHost.Ready)
            try
            {
                shell ??= Start();
            }
            catch (Exception error)
            {
                HopoHost.StartupError = error.GetType().Name + ": " + error.Message;
                CrashReport.Save(error, "Screens");
            }
        if (ApplicationLifetime is IActivityApplicationLifetime activity)
            activity.MainViewFactory = CreateView;
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = CreateView();
        base.OnFrameworkInitializationCompleted();
    }
    private static Shell Start()
    {
        Shell created = new(HopoHost.Store, HopoHost.Identity, false);
        Shell.NotifyHook = (title, text) => AndroidApp.Current?.Notify(title, text);
        Shell.ImportHook = HopoHost.ImportAsync;
        // Finished transfers: received files move to Downloads/hopo.
        created.Engine.Completed += record =>
        {
            if (record.Direction == "Received" && record.State == TransferState.Completed && record.Destination.StartsWith(HopoHost.Inbox, StringComparison.Ordinal) && Directory.Exists(record.Destination))
            {
                int saved = AndroidApp.Current?.SaveToDownloads(record.Destination) ?? 0;
                if (saved > 0)
                    created.Ui(() => created.Tell(saved + (saved == 1 ? " file" : " files") + " saved to Downloads/hopo."));
            }
        };
        return created;
    }
    /// <summary>The shell's screen, moved to the activity shown now (an activity can be recreated while hopo keeps running).</summary>
    private static Control CreateView()
    {
        if (shell == null)
            return new ScrollViewer
            {
                Content = new SelectableTextBlock
                {
                    Text = "hopo could not start.\n\n" + HopoHost.StartupError + "\n\n" + CrashReport.Pending(),
                    Margin = new(24),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
        if (shell.Root.Parent is ContentControl previous)
            previous.Content = null;
        return shell.Root;
    }
}
