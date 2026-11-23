using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace Lantoss.Android;

public sealed class App : Avalonia.Application
{
    public override void Initialize()
    {
        Name = "hopo";
        Styles.Add(new FluentTheme());
    }
    public override void OnFrameworkInitializationCompleted()
    {
        // Avalonia 12 on Android asks for a new screen for each activity; other lifetimes take a single screen.
        if (ApplicationLifetime is IActivityApplicationLifetime activity)
            activity.MainViewFactory = CreateView;
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = CreateView();
        // An error inside the screen is recorded and shown as a message instead of closing the app.
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            CrashReport.Save(e.Exception, "Screen");
            e.Handled = true;
            MainView.Current?.Tell("Something went wrong: " + e.Exception.Message);
        };
        base.OnFrameworkInitializationCompleted();
    }
    private static Control CreateView() => HopoHost.Ready ? new MainView() : new ScrollViewer
    {
        Content = new SelectableTextBlock
        {
            Text = "hopo could not start.\n\n" + HopoHost.StartupError + "\n\n" + CrashReport.Pending(),
            Margin = new(24),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        }
    };
}
