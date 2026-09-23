using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MouseClickCounter
{
    internal static class Program
    {
        private const string MutexName = "Local\\MouseClickCounter.SingleInstance";

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Mouse Click Counter is already running.\nCheck the system tray (notification area).",
                        "Mouse Click Counter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MouseClickCounter");

                try
                {
                    var store = new ClickStore(dataDir);
                    Application.Run(new TrayApplicationContext(store));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Failed to start:\n" + ex.Message,
                        "Mouse Click Counter",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}
