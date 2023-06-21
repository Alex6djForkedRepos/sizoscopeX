using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace sizoscopeX.Core;

static class Utils
{
    public static void ShowWindow(object? content)
    {
        switch (Avalonia.Application.Current)
        {
            case { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop }:
                new FluentAppWindow() { Content = content }.ShowDialog(desktop.MainWindow);
                break;
            case { ApplicationLifetime: ISingleViewApplicationLifetime { MainView: SingleTopView view } }:
                view.NavigateTo(content);
                break;
        }
    }

    public static void SetTitle(string title)
    {
        switch (Avalonia.Application.Current)
        {
            case { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime { MainWindow: FluentAppWindow window } desktop }:
                window.Title = title;
                break;
            case { ApplicationLifetime: ISingleViewApplicationLifetime { MainView: SingleTopView view } }:
                view.Title.Text = title;
                break;
        }
    }

    public static Task<T> TaskRunIfPossible<T>(Func<T> action)
    {
        if (OperatingSystem.IsBrowser())
        {
            return Task.FromResult(action());
        }
        else
        {
            return Task.Run(action);
        }
    }

    public static async Task ContinueOnMainThread<T>(this Task<T> task, Action<T> continuation)
    {
        var result1 = await task;
        await Dispatcher.UIThread.InvokeAsync(() => continuation(result1));
    }
}
