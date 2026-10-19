using Microsoft.UI.Xaml;
using DualAutoClicker.Services;
using System.Diagnostics;

namespace DualAutoClicker;

public partial class App : Application
{
    private Window? _mainWindow;
    private readonly SettingsService _settingsService;
    private readonly ClickerService _clickerService;
    private static bool _shuttingDown;
    private static Mutex? _instanceMutex;

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

        // Last-resort cleanup: if the process is terminated without going through
        // the normal close path, remove the global hooks before the runtime exits.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => StopHooksBestEffort();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Single instance check. A named mutex is much cheaper than the WinRT
        // AppInstance activation path, which costs ~30 ms of the startup budget.
        _instanceMutex = new Mutex(initiallyOwned: true, @"Local\DualAutoClicker.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            Environment.Exit(0);
            return;
        }

        _mainWindow = new MainWindow();
        MainWindow = _mainWindow;

        // Start the clicker service
        _clickerService.Start();

        _mainWindow.Activate();
    }

    /// <summary>
    /// Removes the global hooks and shuts the process down.
    /// The hooks must be removed before the runtime starts tearing the process down:
    /// while a low-level hook is still installed, every mouse/keyboard event in the
    /// system waits for this thread, which is perceived as system-wide input lag.
    /// </summary>
    public static void Shutdown()
    {
        if (_shuttingDown) return;
        _shuttingDown = true;

        try
        {
            // Unhook first, stop automation second.
            ClickerService?.Stop();
            ClickerService?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Shutdown cleanup failed: {ex}");
        }

        try
        {
            MainWindow?.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Window close failed: {ex}");
        }

        Environment.Exit(0);
    }

    private static void StopHooksBestEffort()
    {
        try
        {
            ClickerService?.Stop();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Process exit cleanup failed: {ex}");
        }
    }
}
