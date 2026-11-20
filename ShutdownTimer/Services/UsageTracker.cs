using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 教育分支版：按天统计各软件的使用秒数，持久化到 usage.json。
    /// </summary>
    public static class UsageTracker
    {
        private static readonly object _lock = new();
        private static Dictionary<string, Dictionary<string, int>> _data = new(StringComparer.OrdinalIgnoreCase);
        private static string _today = DateTime.Now.ToString("yyyy-MM-dd");

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Chronos", "usage.json");

        static UsageTracker()
        {
            Load();
        }

        public static int GetSecondsToday(string process)
        {
            lock (_lock)
            {
                EnsureDate();
                if (_data.TryGetValue(_today, out var dict)
                    && dict.TryGetValue(process, out int s))
                    return s;
                return 0;
            }
        }

        public static void AddSeconds(string process, int seconds)
        {
            if (seconds <= 0) return;

            lock (_lock)
            {
                EnsureDate();
                if (!_data.TryGetValue(_today, out var dict))
                {
                    dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    _data[_today] = dict;
                }
                dict.TryGetValue(process, out int cur);
                dict[process] = cur + seconds;
            }

            Save();
        }

        private static void EnsureDate()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_today != today)
            {
                _today = today;
                _data.Clear();
            }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    _data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(
                        File.ReadAllText(FilePath)) ?? new();
                }
            }
            catch { _data = new(); }
        }

        private static void Save()
        {
            try
            {
                string? dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(_data));
            }
            catch { }
        }
    }
}
