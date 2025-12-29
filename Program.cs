using DualAutoClicker.Native;
using DualAutoClicker.Services;

namespace DualAutoClicker;

static class Program
{
    // Unique mutex name for the application
    private const string MutexName = "Global\\DualAutoClicker_SingleInstance_Mutex";
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        // Try to create a mutex to ensure single instance
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            // Another instance is already running
            MessageBox.Show(
                "DualAutoClicker zaten çalışıyor!\n\nUygulama sistem tepsisinde çalışıyor olabilir.",
                "DualAutoClicker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            // Set high DPI mode
            ApplicationConfiguration.Initialize();

            // Set high-resolution timer for the entire application
            MultimediaTimer.SetHighResolution();

            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                // Clean up timer resolution
                MultimediaTimer.ResetResolution();
            }
        }
        finally
        {
            // Release the mutex when the application closes
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }
}
