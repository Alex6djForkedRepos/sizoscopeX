using Avalonia.Controls.ApplicationLifetimes;
using FluentAvalonia.UI.Controls;

namespace sizoscopeX;

class Utils
{
    public static void ShowWindow(object? content)
    {
        switch (Avalonia.Application.Current)
        {
            case { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop }:
                new FluentAppWindow() { Content = content }.ShowDialog(desktop.MainWindow);
                break;
            case { ApplicationLifetime: ISingleViewApplicationLifetime { MainView: Frame frame } }:
                frame.Content = content;
                break;
        }
    }
}
