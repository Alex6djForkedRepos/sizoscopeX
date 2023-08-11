using Avalonia;
using Avalonia.Media;
using sizoscopeX.Core;

namespace sizoscopeX.Desktop;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new MacOSPlatformOptions()
            {
                DisableDefaultApplicationMenuItems = true
            })
            .With(new FontManagerOptions
            {
                DefaultFamilyName = "avares://Avalonia.Fonts.Inter/Assets#Inter"
            });
}