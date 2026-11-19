using System;
using System.IO;
using System.Threading;

namespace ShutdownTimer.Updater
{
    public static class Program
    {
        public static readonly string LogPath =
            Path.Combine(Path.GetTempPath(), "ShutdownTimer_Updater.log");

        public static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}{Environment.NewLine}");
            }
            catch { }
        }

        [STAThread]
        static int Main(string[] args)
        {
            Log("=== Updater started ===");
            Log("args: " + string.Join(" | ", args));

            try
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();

                Microsoft.UI.Xaml.Application.Start((p) =>
                {
                    var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App(args);
                });
            }
            catch (Exception ex)
            {
                Log("FATAL: " + ex);
                return 1;
            }
            return 0;
        }
    }
}