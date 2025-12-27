using DualAutoClicker.Native;
using DualAutoClicker.Services;

namespace DualAutoClicker;

static class Program
{
    [STAThread]
    static void Main()
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
}
