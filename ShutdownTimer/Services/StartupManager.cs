using System;
using System.IO;
using Microsoft.Win32;

namespace ShutdownTimer.Services
{
    public static class StartupManager
    {
        private const string RunKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        // ★ 注册表项名改为 Chronos
        private const string AppName = "Chronos";

        public static void Enable()
        {
            string exe = GetExePath();
            if (string.IsNullOrEmpty(exe)) return;
            string value = $"\"{exe}\" /silent";
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.SetValue(AppName, value);
        }

        public static void Disable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(AppName, throwOnMissingValue: false);
        }

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(AppName) != null;
        }

        private static string GetExePath()
        {
            string? path = Environment.ProcessPath;
            if (path != null &&
                Path.GetFileNameWithoutExtension(path)
                    .Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                path = Path.ChangeExtension(path, ".exe");
            }
            return path ?? string.Empty;
        }
    }
}