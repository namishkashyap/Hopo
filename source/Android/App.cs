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
        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = HopoHost.Ready ? new MainView() : new TextBlock
            {
                Text = "hopo could not start.\n\n" + HopoHost.StartupError,
                Margin = new(24),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
        base.OnFrameworkInitializationCompleted();
    }
}
