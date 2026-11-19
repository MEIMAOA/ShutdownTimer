using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;

namespace ShutdownTimer.Services
{
    public enum StartupLocation
    {
        HKCU,
        HKLM,
        StartupFolder
    }

    public class StartupItem
    {
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string FilePath { get; set; } = "";
        public StartupLocation Location { get; set; }
        public bool IsEnabled { get; set; } = true;

        /// <summary>图标来源文件（exe 或 lnk）</summary>
        public string IconPath { get; set; } = "";

        /// <summary>图标（异步填充，供绑定）</summary>
        public BitmapImage? Icon { get; set; }

        public string LocationDisplay => Location switch
        {
            StartupLocation.HKCU => "当前用户",
            StartupLocation.HKLM => "所有用户",
            StartupLocation.StartupFolder => "启动文件夹",
            _ => ""
        };
    }

    public static class StartupItemManager
    {
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string ApprovedFolderKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

        public static List<StartupItem> GetAll()
        {
            var list = new List<StartupItem>();
            ReadRegistryRun(Registry.CurrentUser, StartupLocation.HKCU, list);
            ReadRegistryRun(Registry.LocalMachine, StartupLocation.HKLM, list);
            ReadStartupFolder(list);
            return list;
        }

        private static void ReadRegistryRun(RegistryKey root, StartupLocation loc, List<StartupItem> list)
        {
            try
            {
                using var runKey = root.OpenSubKey(RunKey);
                if (runKey == null) return;

                var approvedDict = ReadApprovedDict(root);

                foreach (var name in runKey.GetValueNames())
                {
                    var cmd = runKey.GetValue(name)?.ToString() ?? "";
                    bool enabled = true;
                    if (approvedDict.TryGetValue(name, out var bytes) && bytes.Length > 0)
                        enabled = bytes[0] == 0x02;

                    list.Add(new StartupItem
                    {
                        Name = name,
                        Command = cmd,
                        Location = loc,
                        IsEnabled = enabled,
                        IconPath = ResolveIconPathFromCommand(cmd)  // ★
                    });
                }
            }
            catch { }
        }

        private static Dictionary<string, byte[]> ReadApprovedDict(RegistryKey root)
        {
            var dict = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var key = root.OpenSubKey(ApprovedKey);
                if (key == null) return dict;

                foreach (var name in key.GetValueNames())
                {
                    if (key.GetValue(name) is byte[] b)
                        dict[name] = b;
                }
            }
            catch { }
            return dict;
        }

        private static void ReadStartupFolder(List<StartupItem> list)
        {
            try
            {
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!Directory.Exists(folder)) return;

                var approved = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                using (var key = Registry.CurrentUser.OpenSubKey(ApprovedFolderKey))
                {
                    if (key != null)
                    {
                        foreach (var n in key.GetValueNames())
                        {
                            if (key.GetValue(n) is byte[] b)
                                approved[n] = b;
                        }
                    }
                }

                foreach (var file in Directory.GetFiles(folder))
                {
                    var fileName = Path.GetFileName(file);
                    bool enabled = true;
                    if (approved.TryGetValue(fileName, out var bytes) && bytes.Length > 0)
                        enabled = bytes[0] == 0x02;

                    list.Add(new StartupItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Command = file,
                        FilePath = file,
                        Location = StartupLocation.StartupFolder,
                        IsEnabled = enabled,
                        IconPath = file  // ★ 直接是 .lnk 文件，用 SHGetFileInfo 拿图标
                    });
                }
            }
            catch { }
        }

        /// <summary>从启动命令行里解析出 exe 路径</summary>
        public static string ResolveIconPathFromCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return "";

            string s = command.Trim();
            string exePath;

            if (s.StartsWith("\""))
            {
                int end = s.IndexOf('"', 1);
                exePath = end > 0 ? s.Substring(1, end - 1) : s.Trim('"');
            }
            else
            {
                // 未加引号：先尝试把整个字符串当路径（路径本身可能含空格）
                if (File.Exists(s))
                {
                    exePath = s;
                }
                else
                {
                    // 取第一个空格前的部分
                    int sp = s.IndexOf(' ');
                    exePath = sp > 0 ? s.Substring(0, sp) : s;
                }
            }

            // 展开环境变量（如 %ProgramFiles%）
            try
            {
                exePath = Environment.ExpandEnvironmentVariables(exePath);
            }
            catch { }

            return File.Exists(exePath) ? exePath : "";
        }

        // ==================== 修改操作 ====================

        public static bool SetEnabled(StartupItem item, bool enabled)
        {
            try
            {
                if (item.Location == StartupLocation.StartupFolder)
                {
                    using var key = Registry.CurrentUser.CreateSubKey(ApprovedFolderKey);
                    if (key == null) return false;
                    key.SetValue(Path.GetFileName(item.FilePath), MakeApprovedBytes(enabled), RegistryValueKind.Binary);
                    return true;
                }
                else
                {
                    RegistryKey root = item.Location == StartupLocation.HKCU
                        ? Registry.CurrentUser
                        : Registry.LocalMachine;

                    using var key = root.CreateSubKey(ApprovedKey);
                    if (key == null) return false;
                    key.SetValue(item.Name, MakeApprovedBytes(enabled), RegistryValueKind.Binary);
                    return true;
                }
            }
            catch { return false; }
        }

        public static bool Delete(StartupItem item)
        {
            try
            {
                if (item.Location == StartupLocation.StartupFolder)
                {
                    if (File.Exists(item.FilePath))
                        File.Delete(item.FilePath);

                    using var key = Registry.CurrentUser.OpenSubKey(ApprovedFolderKey, writable: true);
                    key?.DeleteValue(Path.GetFileName(item.FilePath), throwOnMissingValue: false);
                    return true;
                }
                else
                {
                    RegistryKey root = item.Location == StartupLocation.HKCU
                        ? Registry.CurrentUser
                        : Registry.LocalMachine;

                    using (var runKey = root.OpenSubKey(RunKey, writable: true))
                    {
                        runKey?.DeleteValue(item.Name, throwOnMissingValue: false);
                    }
                    using (var approvedKey = root.OpenSubKey(ApprovedKey, writable: true))
                    {
                        approvedKey?.DeleteValue(item.Name, throwOnMissingValue: false);
                    }
                    return true;
                }
            }
            catch { return false; }
        }

        private static byte[] MakeApprovedBytes(bool enabled)
        {
            var b = new byte[12];
            b[0] = enabled ? (byte)0x02 : (byte)0x03;
            return b;
        }
    }
}