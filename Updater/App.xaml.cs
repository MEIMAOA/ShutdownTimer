using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;

namespace ShutdownTimer.Updater
{
    public partial class App : Microsoft.UI.Xaml.Application
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

        private readonly string[] _args;
        private Window? _window;

        public App(string[] args)
        {
            _args = args;
            Log("App 构造函数进入");
            this.InitializeComponent();
            Log("InitializeComponent 完成");

            this.UnhandledException += (s, e) =>
            {
                Log("!! UnhandledException: " + e.Message);
                Log("!! 详情: " + e.Exception);
            };
        }

        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Log("OnLaunched");

            try
            {
                _window = new MainWindow(_args);
                ApplyTheme(_window, _args);

                _window.Activate();
                Log("MainWindow 已激活");
            }
            catch (Exception ex)
            {
                Log("!! OnLaunched 异常: " + ex);
                throw;
            }
        }

        private static void ApplyTheme(Window window, string[] args)
        {
            string theme = "System";

            string argTheme = GetArg(args, "--theme");
            if (!string.IsNullOrWhiteSpace(argTheme)) theme = argTheme;

            string appDir = GetArg(args, "--app-dir");
            if (!string.IsNullOrWhiteSpace(appDir))
            {
                try
                {
                    string cfgPath = Path.Combine(appDir.TrimEnd('\\', '/'), "config.json");
                    if (File.Exists(cfgPath))
                    {
                        string json;
                        using (var fs = new FileStream(cfgPath, FileMode.Open, FileAccess.Read,
                            FileShare.ReadWrite, 4096, FileOptions.SequentialScan))
                        using (var sr = new StreamReader(fs, Encoding.UTF8))
                        {
                            json = sr.ReadToEnd();
                        }

                        string cfgTheme = ExtractJsonString(json, "AppTheme");
                        if (!string.IsNullOrWhiteSpace(cfgTheme)) theme = cfgTheme;
                    }
                }
                catch { }
            }

            Log($"主题: {theme}");

            try
            {
                if (window.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = theme switch
                    {
                        "Light" => ElementTheme.Light,
                        "Dark" => ElementTheme.Dark,
                        _ => ElementTheme.Default
                    };
                }
            }
            catch { }
        }

        private static string? ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(key)) return null;
            try
            {
                var pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"";
                var m = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;
            }
            catch { }
            return null;
        }

        private static string? GetArg(string[] args, string key)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
    }
}