using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ShutdownTimer.Pages;
using ShutdownTimer.Services;
using ShutdownTimer.Views;
using Windows.Graphics;

namespace ShutdownTimer
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow? MainWindowInstance { get; private set; }

        private TrayIcon? _trayIcon;
        private bool _hasAppliedInitialSize = false;
        private IntPtr _hwnd = IntPtr.Zero;

        private string? _tutorialShowingTag;
        private bool _onboardingShowing = false;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _scheduleTimer;

        public MainWindow()
        {
            this.InitializeComponent();
            MainWindowInstance = this;

            var config = AppConfig.Load();

            // 语言（必须在本地化之前设置）
            Loc.Set(config.Language);

            // 本地化静态 XAML 文案（导航项另由 ApplyNavLocalization 处理）
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);

            TitleBarText.Text = BuildInfo.AppName;

            SetupSystemBackdrop();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            try
            {
                _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                NativeWindowHelper.EnableLayeredWindow(_hwnd);
            }
            catch { }

            SetupTrayIcon();

            this.Activated += MainWindow_Activated;
            this.AppWindow.Closing += AppWindow_Closing;

            if (this.Content is FrameworkElement rootFe)
                rootFe.ActualThemeChanged += (s, e) => UpdateTitleBarTheme();
            UpdateTitleBarTheme();

            ApplyBackgroundImage(config.BackgroundImagePath);
            ApplyShowBackgroundImage(config.ShowBackgroundImage);
            AppearancePage.ApplyTheme(config.AppTheme);

            LoadUserInfo(config);

            // 导航栏本地化
            ApplyNavLocalization();

            // 本地化导航折叠按钮提示（WinUI 内置文案跟随系统语言）
            NavView.Loaded += (s, ev) => LocalizePaneToggle();
            NavView.PaneOpened += (s, ev) => LocalizePaneToggle();
            NavView.PaneClosed += (s, ev) => LocalizePaneToggle();

            // 教育分支版才显示"软件管控"入口
            EducationNavItem.Visibility = BuildInfo.IsEducation
                ? Visibility.Visible
                : Visibility.Collapsed;

            // 自定时：程序自己计时，到点执行关机/重启/休眠
            SetupScheduleTimer();

            // 教育分支版：启动软件拦截
            try { SoftwareBlocker.Start(); } catch { }

            // 教育分支版：全局 Alt+L 退出
            if (BuildInfo.IsEducation)
            {
                try
                {
                    GlobalHotkey.Register(_hwnd,
                        () => DispatcherQueue.TryEnqueue(() => ProcessGuard.ExitGracefully()));
                }
                catch { }
            }

            // 自动识别微软账户 / 本地账户，填充用户名与头像
            _ = ApplyAccountInfoAsync();

            // 启动时默认打开设置的默认页（默认为主页）
            SelectNavigationItem(string.IsNullOrWhiteSpace(config.DefaultPage)
                ? "home"
                : config.DefaultPage);

            if (!config.OnboardingCompleted)
            {
                DispatcherQueue.TryEnqueue(ShowOnboarding);
            }
            else if (!config.TermsAccepted ||
                     config.AcceptedTermsVersion != TermsOfService.Version)
            {
                DispatcherQueue.TryEnqueue(ShowTermsRequired);
            }
            else
            {
                _ = CheckMicrosoftAccountAsync();
                _ = AutoCheckUpdateAsync();
            }

            // 更新完成后弹出本次更新内容
            DispatcherQueue.TryEnqueue(ShowPostUpdateNotesIfAny);
        }

        // ==================== SystemBackdrop ====================

        private void SetupSystemBackdrop()
        {
            try
            {
                var osVersion = Environment.OSVersion.Version;

                if (osVersion.Build >= 22000)
                {
                    try
                    {
                        this.SystemBackdrop = new MicaBackdrop();
                        return;
                    }
                    catch { }
                }

                if (osVersion.Build >= 17763)
                {
                    try
                    {
                        this.SystemBackdrop = new DesktopAcrylicBackdrop();
                        return;
                    }
                    catch { }
                }
            }
            catch { }
        }

        // ==================== 头像 + 用户名 ====================

        private void LoadUserInfo(AppConfig config)
        {
            try
            {
                string name = string.IsNullOrWhiteSpace(config.UserName)
                    ? (Environment.UserName ?? "时序用户")
                    : config.UserName;
                HeaderUserName.Text = name;

                if (!string.IsNullOrWhiteSpace(config.AvatarPath)
                    && File.Exists(config.AvatarPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage(new Uri(config.AvatarPath));
                        HeaderAvatar.Source = bitmap;
                        HeaderAvatar.Visibility = Visibility.Visible;
                        HeaderAvatarPlaceholder.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        HeaderAvatar.Visibility = Visibility.Collapsed;
                        HeaderAvatarPlaceholder.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    HeaderAvatar.Visibility = Visibility.Collapsed;
                    HeaderAvatarPlaceholder.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        public void RefreshUserInfo()
        {
            try
            {
                var config = AppConfig.Load();
                LoadUserInfo(config);
            }
            catch { }
        }

        // ==================== 自动识别账户 ====================

        private async Task ApplyAccountInfoAsync()
        {
            try
            {
                var info = await MicrosoftAccountService.GetAccountInfoAsync();
                if (info == null) return;

                var config = AppConfig.Load();
                bool changed = false;

                // 用户名：未手动设置时，用微软账户显示名或本地用户名
                if (string.IsNullOrWhiteSpace(config.UserName)
                    && !string.IsNullOrWhiteSpace(info.EffectiveName))
                {
                    config.UserName = info.EffectiveName.Trim();
                    changed = true;
                }

                // 头像：仅微软账户才自动设置（本地账户保持无头像）
                if (info.IsMicrosoftAccount
                    && string.IsNullOrWhiteSpace(config.AvatarPath))
                {
                    string? avatarPath = SaveAvatarFromBase64(info.AvatarBase64);
                    if (!string.IsNullOrWhiteSpace(avatarPath))
                    {
                        config.AvatarPath = avatarPath;
                        changed = true;
                    }
                }

                if (changed)
                {
                    config.Save();
                    LoadUserInfo(config);
                }
            }
            catch { }
        }

        private static string? SaveAvatarFromBase64(string? base64)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64)) return null;
                byte[] bytes = Convert.FromBase64String(base64);
                if (bytes.Length == 0) return null;

                string configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Chronos");
                Directory.CreateDirectory(configDir);

                string path = Path.Combine(configDir, "avatar_ms.png");
                File.WriteAllBytes(path, bytes);
                return path;
            }
            catch { return null; }
        }

        // ==================== 关机计划刷新 ====================

        private void SetupScheduleTimer()
        {
            try
            {
                _scheduleTimer = DispatcherQueue.CreateTimer();
                _scheduleTimer.Interval = TimeSpan.FromSeconds(10);
                _scheduleTimer.IsRepeating = true;
                _scheduleTimer.Tick += (s, e) =>
                {
                    try { ShutdownScheduler.Tick(); } catch { }
                    try { SoftwareBlocker.Tick(); } catch { }
                    try { ProcessGuard.MainTick(); } catch { }
                };
                _scheduleTimer.Start();
            }
            catch { }
        }

        // ==================== 用户使用条款 ====================

        private void ShowTermsRequired()
        {
            _onboardingShowing = true;

            var overlay = new TermsOverlay
            {
                ShowDeclineButton = true
            };

            overlay.Accepted += (s, e) =>
            {
                OverlayHost.Children.Clear();
                OverlayHost.Visibility = Visibility.Collapsed;
                _onboardingShowing = false;

                var c = AppConfig.Load();
                c.TermsAccepted = true;
                c.AcceptedTermsVersion = TermsOfService.Version;
                c.Save();

                _ = CheckMicrosoftAccountAsync();
                _ = AutoCheckUpdateAsync();
            };

            overlay.Declined += (s, e) =>
            {
                Microsoft.UI.Xaml.Application.Current.Exit();
            };

            OverlayHost.Children.Clear();
            OverlayHost.Children.Add(overlay);
            OverlayHost.Visibility = Visibility.Visible;
        }

        // ==================== 首次配置 ====================

        private void ShowOnboarding()
        {
            _onboardingShowing = true;

            var overlay = new OnboardingOverlay();
            overlay.Completed += (s, e) =>
            {
                OverlayHost.Children.Clear();
                OverlayHost.Visibility = Visibility.Collapsed;
                _onboardingShowing = false;

                var c = AppConfig.Load();
                ApplyBackgroundImage(c.BackgroundImagePath);
                ApplyShowBackgroundImage(c.ShowBackgroundImage);
                AppearancePage.ApplyTheme(c.AppTheme);

                LoadUserInfo(c);

                ContentFrame.Navigate(typeof(GeneralPage));

                if (!c.TermsAccepted ||
                    c.AcceptedTermsVersion != TermsOfService.Version)
                {
                    DispatcherQueue.TryEnqueue(ShowTermsRequired);
                }
                else
                {
                    _ = CheckMicrosoftAccountAsync();
                    _ = AutoCheckUpdateAsync();
                }
            };

            OverlayHost.Children.Clear();
            OverlayHost.Children.Add(overlay);
            OverlayHost.Visibility = Visibility.Visible;
        }

        // ==================== 更新完成后弹窗 ====================

        private async void ShowPostUpdateNotesIfAny()
        {
            try
            {
                // 等待窗口内容加载完成，确保 XamlRoot 可用
                await Task.Delay(700);

                var notes = UpdateService.LoadAndClearPendingUpdateNotes();
                if (notes == null || string.IsNullOrWhiteSpace(notes.Version)) return;

                string versionText = notes.Version.Trim();
                if (!versionText.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    versionText = "v" + versionText;

                string cleanNotes = UpdateService.CleanReleaseNotes(notes.Notes);
                string content = versionText + "\n\n" + Loc.T("更新内容：") + "\n" + cleanNotes;

                var dialog = new ContentDialog
                {
                    Title = Loc.T("更新完成"),
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = content,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 480
                        },
                        MaxHeight = 420
                    },
                    CloseButtonText = Loc.T("确定"),
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = (Content as FrameworkElement)?.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch { }
        }

        // ==================== 自动检查更新 ====================

        private async Task AutoCheckUpdateAsync()
        {
            try
            {
                await Task.Delay(3000);

                var update = await UpdateService.CheckForUpdateAsync();

                if (update == null) return;
                if (!string.IsNullOrEmpty(update.ErrorMessage)) return;
                if (!update.HasUpdate) return;

                var dialog = new ContentDialog
                {
                    Title = Loc.T("发现新版本"),
                    Content = Loc.T("当前版本：") + "v" + update.CurrentVersion + "\n" +
                              Loc.T("最新版本：") + update.LatestVersion + "\n\n" +
                              Loc.T("更新内容：") + "\n" + update.ReleaseNotes + "\n\n" +
                              Loc.T("是否前往「检查更新」页面？"),
                    PrimaryButtonText = Loc.T("前往更新"),
                    CloseButtonText = Loc.T("稍后"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        foreach (var item in NavView.MenuItems)
                        {
                            if (item is NavigationViewItem nvi &&
                                nvi.Tag?.ToString() == "update")
                            {
                                NavView.SelectedItem = nvi;
                                break;
                            }
                        }
                    });
                }
            }
            catch { }
        }

        // ==================== 微软账户 ====================

        private async Task CheckMicrosoftAccountAsync()
        {
            try
            {
                await Task.Delay(600);

                bool isMs = await MicrosoftAccountService.IsMicrosoftAccountAsync();
                if (isMs) return;

                var dialog = new ContentDialog
                {
                    Title = Loc.T("建议登录微软账户"),
                    Content = Loc.T("检测到你当前使用的是本地账户。\n\n登录微软账户可以：\n• 跨设备同步设置和文件\n• 更方便地找回密码\n• 使用 OneDrive 云存储\n\n是否现在去系统设置？"),
                    PrimaryButtonText = Loc.T("去登录"),
                    CloseButtonText = Loc.T("稍后"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
                };

                var result = await dialog.ShowAsync();
                MsAccountInfoBar.IsOpen = true;

                if (result == ContentDialogResult.Primary)
                    OpenMsAccountSettings();
            }
            catch { }
        }

        private void LoginMsAccount_Click(object sender, RoutedEventArgs e)
            => OpenMsAccountSettings();

        private void OpenMsAccountSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:yourinfo",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        // ==================== 窗口透明度 ====================

        private void SetWindowAlpha(byte alpha)
        {
            if (_hwnd != IntPtr.Zero)
                NativeWindowHelper.SetWindowOpacity(_hwnd, alpha);
        }

        public void ApplyShowBackgroundImage(bool show)
        {
            if (show)
            {
                BackgroundImage.Visibility = Visibility.Visible;
                BackgroundMask.Visibility = Visibility.Visible;
                SetWindowAlpha(255);
            }
            else
            {
                BackgroundImage.Visibility = Visibility.Collapsed;
                BackgroundMask.Visibility = Visibility.Collapsed;
                var c = AppConfig.Load();
                byte a = (byte)Math.Clamp(c.WindowAlpha, 30, 255);
                SetWindowAlpha(a);
            }
        }

        public void ApplyBackgroundImage(string? customPath)
        {
            string? finalPath = null;

            if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
                finalPath = customPath;
            else
            {
                string def = AppConfig.GetDefaultBackgroundPath();
                if (File.Exists(def)) finalPath = def;
            }

            if (finalPath == null) { BackgroundImage.Source = null; return; }

            try { BackgroundImage.Source = new BitmapImage(new Uri(finalPath)); }
            catch { BackgroundImage.Source = null; }
        }

        // ==================== 标题栏 ====================

        private void UpdateTitleBarTheme()
        {
            try
            {
                var tb = this.AppWindow.TitleBar;
                bool isDark = (this.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;

                var transparent = Windows.UI.Color.FromArgb(0, 0, 0, 0);

                var hoverBg = isDark
                    ? Windows.UI.Color.FromArgb(40, 255, 255, 255)
                    : Windows.UI.Color.FromArgb(40, 0, 0, 0);
                var pressedBg = isDark
                    ? Windows.UI.Color.FromArgb(70, 255, 255, 255)
                    : Windows.UI.Color.FromArgb(70, 0, 0, 0);

                var fg = isDark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black;
                var inactiveFg = isDark
                    ? Windows.UI.Color.FromArgb(255, 128, 128, 128)
                    : Windows.UI.Color.FromArgb(255, 150, 150, 150);

                tb.BackgroundColor = transparent;
                tb.InactiveBackgroundColor = transparent;
                tb.ForegroundColor = fg;
                tb.InactiveForegroundColor = inactiveFg;

                tb.ButtonBackgroundColor = transparent;
                tb.ButtonInactiveBackgroundColor = transparent;
                tb.ButtonHoverBackgroundColor = hoverBg;
                tb.ButtonHoverForegroundColor = fg;
                tb.ButtonPressedBackgroundColor = pressedBg;
                tb.ButtonPressedForegroundColor = fg;
                tb.ButtonForegroundColor = fg;
                tb.ButtonInactiveForegroundColor = inactiveFg;
            }
            catch { }
        }

        // ==================== 窗口状态 ====================

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (!_hasAppliedInitialSize &&
                e.WindowActivationState != WindowActivationState.Deactivated)
            {
                RestoreWindowBounds();
                _hasAppliedInitialSize = true;
            }
        }

        private void RestoreWindowBounds()
        {
            try
            {
                var config = AppConfig.Load();

                if (!config.OnboardingCompleted)
                {
                    const int w = 1280;
                    const int h = 720;
                    this.AppWindow.Resize(new SizeInt32(w, h));

                    try
                    {
                        var display = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                            this.AppWindow.Id,
                            Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                        if (display != null)
                        {
                            var work = display.WorkArea;
                            int x = work.X + (work.Width - w) / 2;
                            int y = work.Y + (work.Height - h) / 2;
                            this.AppWindow.Move(new PointInt32(x, y));
                        }
                    }
                    catch { }
                    return;
                }

                int ww = config.WindowWidth > 0 ? config.WindowWidth : 900;
                int hh = config.WindowHeight > 0 ? config.WindowHeight : 680;
                this.AppWindow.Resize(new SizeInt32(ww, hh));

                if (config.WindowX >= 0 && config.WindowY >= 0)
                    this.AppWindow.Move(new PointInt32(config.WindowX, config.WindowY));
            }
            catch { }
        }

        private void SaveWindowBounds()
        {
            try
            {
                var config = AppConfig.Load();
                var size = this.AppWindow.Size;
                config.WindowWidth = size.Width;
                config.WindowHeight = size.Height;
                var pos = this.AppWindow.Position;
                config.WindowX = pos.X;
                config.WindowY = pos.Y;
                config.Save();
            }
            catch { }
        }

        // ==================== 托盘 ====================

        private void SetupTrayIcon()
        {
            _trayIcon = new TrayIcon(BuildInfo.AppName);
            _trayIcon.LeftClick += () => DispatcherQueue.TryEnqueue(RestoreWindow);
            _trayIcon.ShowRequested += () => DispatcherQueue.TryEnqueue(RestoreWindow);
            _trayIcon.ExitRequested += () => DispatcherQueue.TryEnqueue(() =>
            {
                _trayIcon?.Dispose();
                ProcessGuard.ExitGracefully();
            });
        }

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender,
            Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            SaveWindowBounds();
            var cfg = AppConfig.Load();
            if (cfg.ShowInTray)
            {
                args.Cancel = true;
                this.AppWindow.Hide();
            }
            else
            {
                _trayIcon?.Dispose();
                ProcessGuard.ExitGracefully();
            }
        }

        public void RestoreWindow()
        {
            try { this.AppWindow.Show(); this.Activate(); } catch { }
        }

        /// <summary>根据 tag 选中并导航到指定页；找不到则回退到主页。</summary>
        private void SelectNavigationItem(string tag)
        {
            NavigationViewItem? target = null;

            foreach (var menuItem in NavView.MenuItems)
            {
                if (menuItem is NavigationViewItem nvi && nvi.Tag?.ToString() == tag)
                {
                    target = nvi;
                    break;
                }
            }

            if (target == null)
            {
                foreach (var footerItem in NavView.FooterMenuItems)
                {
                    if (footerItem is NavigationViewItem nvi && nvi.Tag?.ToString() == tag)
                    {
                        target = nvi;
                        break;
                    }
                }
            }

            NavView.SelectedItem = target ?? NavView.MenuItems[0];
        }

        /// <summary>导航项 tag → 中文文本。</summary>
        private static string NavText(string tag) => tag switch
        {
            "home" => "主页",
            "general" => "定时关机",
            "startup" => "启动项",
            "apps" => "应用",
            "sysstatus" => "系统状态",
            "cleanup" => "清理优化",
            "network" => "网络连接",
            "monitoring" => "系统监控",
            "tools" => "常用工具",
            "security" => "安全隐私",
            "automation" => "自动化",
            "appearance" => "外观体验",
            "update" => "检查更新",
            "education" => "软件管控",
            "about" => "关于",
            "settings" => "设置",
            _ => ""
        };

        /// <summary>按当前语言更新导航项文字。</summary>
        private void ApplyNavLocalization()
        {
            foreach (var menuItem in NavView.MenuItems)
            {
                if (menuItem is NavigationViewItem nvi && nvi.Tag is string tag)
                {
                    string zh = NavText(tag);
                    if (zh.Length > 0) nvi.Content = Loc.T(zh);
                }
            }

            foreach (var footerItem in NavView.FooterMenuItems)
            {
                if (footerItem is NavigationViewItem nvi && nvi.Tag is string tag)
                {
                    string zh = NavText(tag);
                    if (zh.Length > 0) nvi.Content = Loc.T(zh);
                }
            }
        }

        /// <summary>本地化导航折叠（汉堡）按钮的提示与无障碍名称。</summary>
        private void LocalizePaneToggle()
        {
            try
            {
                var toggle = FindNamedElement(NavView, "TogglePaneButton");
                if (toggle == null) return;
                string text = Loc.T("导航");
                ToolTipService.SetToolTip(toggle, text);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(toggle, text);
            }
            catch { }
        }

        private static FrameworkElement? FindNamedElement(DependencyObject root, string name)
        {
            if (root is FrameworkElement fe && fe.Name == name) return fe;
            int n = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var r = FindNamedElement(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>切换语言后刷新导航与当前页。</summary>
        public void RefreshLanguage()
        {
            Loc.Set(AppConfig.Load().Language);
            ApplyNavLocalization();
            LocalizePaneToggle();

            if (ContentFrame.CurrentSourcePageType is Type t)
            {
                ContentFrame.Navigate(t);
            }
        }

        // ==================== 导航 + 教程 ====================

        private void NavView_SelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item) return;

            string tag = item.Tag?.ToString() ?? string.Empty;
            Type? pageType = tag switch
            {
                "home" => typeof(HomePage),
                "general" => typeof(GeneralPage),
                "startup" => typeof(StartupPage),
                "apps" => typeof(AppsPage),
                "sysstatus" => typeof(SystemStatusPage),
                "cleanup" => typeof(CleanupPage),
                "network" => typeof(NetworkPage),
                "monitoring" => typeof(MonitoringPage),
                "tools" => typeof(ToolsPage),
                "security" => typeof(SecurityPage),
                "automation" => typeof(AutomationPage),
                "appearance" => typeof(AppearancePage),
                "update" => typeof(UpdatePage),
                "education" => typeof(EducationPage),
                "about" => typeof(AboutPage),
                "settings" => typeof(SettingsPage),
                _ => null
            };

            if (pageType == null) return;

            if (ContentFrame.CurrentSourcePageType != pageType)
                ContentFrame.Navigate(pageType);

            if (_onboardingShowing) return;

            DispatcherQueue.TryEnqueue(() => CheckAndShowTutorial(tag));
        }

        private void CheckAndShowTutorial(string tag)
        {
            if (TutorialService.GetTutorial(tag).Count == 0) return;
            if (TutorialService.IsCompleted(tag)) return;
            if (_tutorialShowingTag == tag) return;
            if (_onboardingShowing) return;

            _tutorialShowingTag = tag;

            var overlay = new TutorialOverlay();
            overlay.Completed += (s, e) =>
            {
                OverlayHost.Children.Clear();
                OverlayHost.Visibility = Visibility.Collapsed;
                _tutorialShowingTag = null;
            };

            OverlayHost.Children.Clear();
            OverlayHost.Children.Add(overlay);
            OverlayHost.Visibility = Visibility.Visible;
            overlay.Start(tag);
        }
    }
}