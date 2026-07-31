using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace sizoscopeX.Core;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var view = new MainView();
            desktop.MainWindow = new FluentAppWindow
            {
                Content = view,
                Title = "sizoscopeX"
            };
            switch (desktop.Args)
            {
                case [string baseFileName, string diffFileName, ..] when File.Exists(baseFileName) && File.Exists(diffFileName):
                    await Dispatcher.UIThread.InvokeAsync(() => view.OpenFromPathAsync(baseFileName, diffFileName));
                    break;
                case [string fileName, ..] when File.Exists(fileName):
                    await Dispatcher.UIThread.InvokeAsync(() => view.OpenFromPathAsync(fileName));
                    break;
            }
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