using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace sizoscopeX;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new FluentAppWindow
            {
                Content = new MainView(),
                Title = "sizoscopeX"
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new SingleTopView
            {
                ContentFrame =
                {
                    Content = new MainView()
                }
            };
        };

        base.OnFrameworkInitializationCompleted();
    }
}