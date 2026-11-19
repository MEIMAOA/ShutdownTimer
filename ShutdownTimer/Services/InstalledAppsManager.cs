using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;

namespace ShutdownTimer.Services
{
    public class InstalledApp
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string InstallLocation { get; set; } = "";
        public string UninstallString { get; set; } = "";
        public string IconPath { get; set; } = "";

        /// <summary>图标（异步填充，供 UI 绑定）</summary>
        public BitmapImage? Icon { get; set; }
    }

    public static class InstalledAppsManager
    {
        private const string UninstallKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string UninstallKeyWow =
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        public static List<InstalledApp> GetAll()
        {
            var list = new List<InstalledApp>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ReadFrom(Registry.LocalMachine, UninstallKey, list, seen);
            ReadFrom(Registry.LocalMachine, UninstallKeyWow, list, seen);
            ReadFrom(Registry.CurrentUser, UninstallKey, list, seen);

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        private static void ReadFrom(RegistryKey root, string path,
            List<InstalledApp> list, HashSet<string> seen)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null) return;

                foreach (var subName in key.GetSubKeyNames())
                {
                    using var sub = key.OpenSubKey(subName);
                    if (sub == null) continue;

                    var name = sub.GetValue("DisplayName")?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var systemComponent = sub.GetValue("SystemComponent")?.ToString();
                    if (systemComponent == "1") continue;

                    var parent = sub.GetValue("ParentKeyName")?.ToString();
                    if (!string.IsNullOrEmpty(parent)) continue;

                    if (seen.Contains(name)) continue;
                    seen.Add(name);

                    string installLoc = sub.GetValue("InstallLocation")?.ToString() ?? "";
                    string displayIcon = sub.GetValue("DisplayIcon")?.ToString() ?? "";
                    string uninstallStr = sub.GetValue("UninstallString")?.ToString() ?? "";

                    list.Add(new InstalledApp
                    {
                        Name = name,
                        Version = sub.GetValue("DisplayVersion")?.ToString() ?? "",
                        Publisher = sub.GetValue("Publisher")?.ToString() ?? "",
                        InstallLocation = installLoc,
                        UninstallString = uninstallStr,
                        IconPath = ResolveIconPath(displayIcon, installLoc)
                    });
                }
            }
            catch { }
        }

        /// <summary>从 DisplayIcon 或 InstallLocation 找出一个可提取图标的 exe 路径</summary>
        private static string ResolveIconPath(string displayIcon, string installLocation)
        {
            // 1. 优先 DisplayIcon
            var exe = ExtractExeFromDisplayIcon(displayIcon);
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
                return exe;

            // 2. 从 InstallLocation 找 exe
            if (!string.IsNullOrWhiteSpace(installLocation) && Directory.Exists(installLocation))
            {
                try
                {
                    var exes = Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly);
                    if (exes.Length > 0) return exes[0];
                }
                catch { }
            }

            return "";
        }

        /// <summary>从 DisplayIcon 字符串里解析出 exe 路径（去掉 ,索引 和引号）</summary>
        private static string ExtractExeFromDisplayIcon(string displayIcon)
        {
            if (string.IsNullOrWhiteSpace(displayIcon)) return "";

            string s = displayIcon.Trim();

            // 去掉末尾的 ",0" 或 ",-1"
            int comma = s.LastIndexOf(',');
            if (comma > 0 && int.TryParse(s.Substring(comma + 1).Trim(), out _))
                s = s.Substring(0, comma);

            // 去掉引号
            s = s.Trim('"').Trim();

            // 若指向目录，返回空（交给 InstallLocation 处理）
            if (Directory.Exists(s)) return "";

            // 只返回 .exe
            if (s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return s;

            return "";
        }
    }
}