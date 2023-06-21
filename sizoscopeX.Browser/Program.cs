using System.Runtime.Versioning;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Browser;
using sizoscopeX.Core;

[assembly: SupportedOSPlatform("browser")]

namespace sizoscopeX.Browser;

internal partial class Program
{
    private static async Task Main(string[] args) => await BuildAvaloniaApp()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
