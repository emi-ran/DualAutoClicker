using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using DualAutoClicker.Services;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace DualAutoClicker;

public partial class App : Application
{
    private Window? _mainWindow;
    private readonly SettingsService _settingsService;
    private readonly ClickerService _clickerService;

    public static SettingsService SettingsService { get; private set; } = null!;
    public static ClickerService ClickerService { get; private set; } = null!;
    public static Window MainWindow { get; private set; } = null!;

    public App()
    {
        this.InitializeComponent();

        // Initialize services
        _settingsService = new SettingsService();
        _settingsService.Load();
        SettingsService = _settingsService;

        _clickerService = new ClickerService(_settingsService);
        ClickerService = _clickerService;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Single instance check
        var mainInstance = AppInstance.FindOrRegisterForKey("DualAutoClicker");
        if (!mainInstance.IsCurrent)
        {
            // Another instance is running, activate it and exit
            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            mainInstance.RedirectActivationToAsync(activatedArgs).AsTask().Wait();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            return;
        }

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;

        // Start the clicker service
        _clickerService.Start();

        _mainWindow.Activate();
    }

    public static void Shutdown()
    {
        ClickerService?.Stop();
        ClickerService?.Dispose();
        MainWindow?.Close();
        Environment.Exit(0);
    }
}
