using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ShutdownTimer.Services
{
    public class AppConfig
    {
        // 定时关机
        public bool Enabled { get; set; } = false;
        public int ShutdownHour { get; set; } = 23;
        public int ShutdownMinute { get; set; } = 0;

        /// <summary>关机时间段列表（每个元素为 "HH:mm"）。null 表示旧版配置，需迁移。</summary>
        public List<string>? ShutdownTimes { get; set; }

        // 外观
        public bool ShowBackgroundImage { get; set; } = true;
        public string BackgroundImagePath { get; set; } = "";
        public string AppTheme { get; set; } = "System";
        public int WindowAlpha { get; set; } = 200;

        // 系统集成
        public bool ShowInTray { get; set; } = true;

        // 窗口
        public int WindowWidth { get; set; } = 0;
        public int WindowHeight { get; set; } = 0;
        public int WindowX { get; set; } = -1;
        public int WindowY { get; set; } = -1;

        // 自动化
        public int AutoLockMinutes { get; set; } = 0;
        public bool AutoRestartEnabled { get; set; } = false;
        public int AutoRestartHour { get; set; } = 3;
        public int AutoRestartMinute { get; set; } = 0;
        public bool AutoHibernateEnabled { get; set; } = false;
        public int AutoHibernateHour { get; set; } = 1;
        public int AutoHibernateMinute { get; set; } = 0;

        // 引导与教程
        public bool OnboardingCompleted { get; set; } = false;
        public List<string> CompletedTutorials { get; set; } = new();
        public string PersonalizationType { get; set; } = "Custom";
        public string UserName { get; set; } = "";
        public string AvatarPath { get; set; } = "";

        // 用户使用条款
        public bool TermsAccepted { get; set; } = false;
        public string AcceptedTermsVersion { get; set; } = "";

        // ==================== 更新 ====================

        /// <summary>更新渠道：Official（正式版）或 Education（教育分支版）。</summary>
        public string UpdateChannel { get; set; } = BuildInfo.DefaultChannel;

        /// <summary>教育分支版：禁止打开的软件（进程名列表）。</summary>
        public List<string> BlockedApps { get; set; } = new();

        // ==================== 危险选项（锁定）====================

        /// <summary>锁定定时关机：定时关机页所有控件禁用</summary>
        public bool LockShutdown { get; set; } = false;

        /// <summary>锁定托盘显示：设置页托盘开关禁用</summary>
        public bool LockTrayIcon { get; set; } = false;

        /// <summary>锁定用户名和头像：设置页用户卡片编辑按钮禁用</summary>
        public bool LockUserInfo { get; set; } = false;

        /// <summary>锁定窗口透明度：设置页透明度滑块禁用</summary>
        public bool LockWindowOpacity { get; set; } = false;

        /// <summary>锁定背景图：设置页背景图相关控件禁用</summary>
        public bool LockBackgroundImage { get; set; } = false;

        /// <summary>锁定教程重置：设置页教程重置按钮禁用</summary>
        public bool LockTutorials { get; set; } = false;

        // ==================== 配置读写 ====================

        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Chronos");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        public static string GetConfigPath() => ConfigPath;

        public static string GetDefaultBackgroundPath()
            => Path.Combine(AppContext.BaseDirectory, "Assets", "1.png");

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new AppConfig();
                string json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                Migrate(cfg);
                return cfg;
            }
            catch { return new AppConfig(); }
        }

        /// <summary>旧版只有单个关机时间，迁移为时间段列表。</summary>
        private static void Migrate(AppConfig cfg)
        {
            if (cfg.ShutdownTimes == null)
            {
                cfg.ShutdownTimes = new List<string>
                {
                    $"{Math.Clamp(cfg.ShutdownHour, 0, 23):D2}:{Math.Clamp(cfg.ShutdownMinute, 0, 59):D2}"
                };
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                string json = JsonSerializer.Serialize(this,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}