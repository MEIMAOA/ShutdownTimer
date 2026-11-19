using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using ShutdownTimer.Services;
using Windows.Storage.Pickers;

namespace ShutdownTimer.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private bool _isLoading = false;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            _isLoading = true;
            try
            {
                var c = AppConfig.Load();

                // 用户信息
                LoadUserInfo(c);

                // 其它设置
                if (AlphaSlider != null) AlphaSlider.Value = c.WindowAlpha;
                if (AlphaValueText != null) AlphaValueText.Text = c.WindowAlpha.ToString();
                if (BackgroundSwitch != null) BackgroundSwitch.IsOn = c.ShowBackgroundImage;

                UpdatePathText(c.BackgroundImagePath);
                UpdatePreview(c.BackgroundImagePath);

                if (TraySwitch != null) TraySwitch.IsOn = c.ShowInTray;

                // ★ 危险选项锁定状态
                if (LockShutdownSwitch != null) LockShutdownSwitch.IsOn = c.LockShutdown;
                if (LockTrayIconSwitch != null) LockTrayIconSwitch.IsOn = c.LockTrayIcon;
                if (LockUserInfoSwitch != null) LockUserInfoSwitch.IsOn = c.LockUserInfo;
                if (LockWindowOpacitySwitch != null) LockWindowOpacitySwitch.IsOn = c.LockWindowOpacity;
                if (LockBackgroundImageSwitch != null) LockBackgroundImageSwitch.IsOn = c.LockBackgroundImage;
                if (LockTutorialsSwitch != null) LockTutorialsSwitch.IsOn = c.LockTutorials;

                // ★ 应用锁定效果到本页控件
                ApplyLockState(c);
            }
            catch { }
            finally { _isLoading = false; }
        }

        // ==================== 锁定效果 ====================

        /// <summary>
        /// 根据锁定状态禁用本页对应控件
        /// </summary>
        private void ApplyLockState(AppConfig c)
        {
            try
            {
                // 用户信息
                EditNameButton.IsEnabled = !c.LockUserInfo;
                ChangeAvatarButton.IsEnabled = !c.LockUserInfo;
                UserInfoLockHint.Visibility = c.LockUserInfo ? Visibility.Visible : Visibility.Collapsed;

                // 透明度
                AlphaSlider.IsEnabled = !c.LockWindowOpacity;

                // 背景图
                BackgroundSwitch.IsEnabled = !c.LockBackgroundImage;
                ChooseBackgroundButton.IsEnabled = !c.LockBackgroundImage;
                ResetBackgroundButton.IsEnabled = !c.LockBackgroundImage;

                // 教程
                ResetTutorialsButton.IsEnabled = !c.LockTutorials;

                // 托盘
                TraySwitch.IsEnabled = !c.LockTrayIcon;
            }
            catch { }
        }

        // ==================== 用户信息 ====================

        private void LoadUserInfo(AppConfig c)
        {
            try
            {
                string name = string.IsNullOrWhiteSpace(c.UserName)
                    ? (Environment.UserName ?? "时序用户")
                    : c.UserName;
                SettingsUserName.Text = name;

                if (!string.IsNullOrWhiteSpace(c.AvatarPath)
                    && File.Exists(c.AvatarPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage(new Uri(c.AvatarPath));
                        SettingsAvatar.Source = bitmap;
                        SettingsAvatar.Visibility = Visibility.Visible;
                        SettingsAvatarPlaceholder.Visibility = Visibility.Collapsed;
                    }
                    catch
                    {
                        SettingsAvatar.Visibility = Visibility.Collapsed;
                        SettingsAvatarPlaceholder.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    SettingsAvatar.Visibility = Visibility.Collapsed;
                    SettingsAvatarPlaceholder.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        private async void EditName_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var c = AppConfig.Load();
                if (c.LockUserInfo) return;  // 双重保险

                var textBox = new TextBox
                {
                    Text = c.UserName ?? "",
                    PlaceholderText = "请输入新的用户名（1-20 字）",
                    MaxLength = 20,
                    SelectionStart = (c.UserName ?? "").Length
                };

                var dialog = new ContentDialog
                {
                    Title = "编辑用户名",
                    Content = textBox,
                    PrimaryButtonText = "保存",
                    CloseButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary) return;

                string newName = textBox.Text.Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    var errDialog = new ContentDialog
                    {
                        Title = "用户名不能为空",
                        Content = "请重新输入用户名。",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await errDialog.ShowAsync();
                    return;
                }

                c = AppConfig.Load();
                c.UserName = newName;
                c.Save();

                SettingsUserName.Text = newName;
                MainWindow.MainWindowInstance?.RefreshUserInfo();
            }
            catch { }
        }

        private async void ChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var c = AppConfig.Load();
                if (c.LockUserInfo) return;

                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".bmp");
                picker.FileTypeFilter.Add(".gif");

                var hwnd = WinRT.Interop.WindowNative
                    .GetWindowHandle(MainWindow.MainWindowInstance);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                string savedPath = SaveAvatarToConfigDir(file.Path);

                c = AppConfig.Load();
                c.AvatarPath = savedPath;
                c.Save();

                LoadUserInfo(c);
                MainWindow.MainWindowInstance?.RefreshUserInfo();
            }
            catch { }
        }

        private static string SaveAvatarToConfigDir(string srcPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(srcPath) || !File.Exists(srcPath))
                    return srcPath;

                string configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Chronos");
                Directory.CreateDirectory(configDir);

                string ext = Path.GetExtension(srcPath);
                if (string.IsNullOrEmpty(ext)) ext = ".png";

                string dst = Path.Combine(configDir, "avatar" + ext);

                if (string.Equals(Path.GetFullPath(srcPath),
                                  Path.GetFullPath(dst),
                                  StringComparison.OrdinalIgnoreCase))
                    return dst;

                File.Copy(srcPath, dst, overwrite: true);
                return dst;
            }
            catch
            {
                return srcPath;
            }
        }

        // ==================== 透明度 ====================

        private void AlphaSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isLoading) return;
            if (AlphaValueText == null) return;

            try
            {
                int alpha = (int)e.NewValue;
                AlphaValueText.Text = alpha.ToString();

                var c = AppConfig.Load();
                c.WindowAlpha = alpha;
                c.Save();

                if (!c.ShowBackgroundImage)
                    MainWindow.MainWindowInstance?.ApplyShowBackgroundImage(false);
            }
            catch { }
        }

        // ==================== 背景图 ====================

        private void BackgroundSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            if (BackgroundSwitch == null) return;
            if (MainWindow.MainWindowInstance == null) return;

            try
            {
                bool show = BackgroundSwitch.IsOn;
                MainWindow.MainWindowInstance.ApplyShowBackgroundImage(show);

                var c = AppConfig.Load();
                c.ShowBackgroundImage = show;
                c.Save();
            }
            catch { }
        }

        private async void ChooseBackground_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var c = AppConfig.Load();
                if (c.LockBackgroundImage) return;

                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".bmp");

                var hwnd = WinRT.Interop.WindowNative
                    .GetWindowHandle(MainWindow.MainWindowInstance);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                MainWindow.MainWindowInstance?.ApplyBackgroundImage(file.Path);
                UpdatePathText(file.Path);
                UpdatePreview(file.Path);

                c = AppConfig.Load();
                c.BackgroundImagePath = file.Path;
                c.Save();
            }
            catch { }
        }

        private void ResetBackground_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var c = AppConfig.Load();
                if (c.LockBackgroundImage) return;

                MainWindow.MainWindowInstance?.ApplyBackgroundImage("");
                UpdatePathText("");
                UpdatePreview("");

                c = AppConfig.Load();
                c.BackgroundImagePath = "";
                c.Save();
            }
            catch { }
        }

        private void UpdatePathText(string path)
        {
            if (BackgroundPathText == null) return;
            BackgroundPathText.Text =
                (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    ? path
                    : $"默认：{AppConfig.GetDefaultBackgroundPath()}";
        }

        private void UpdatePreview(string path)
        {
            if (PreviewImage == null) return;

            try
            {
                string? p = null;
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) p = path;
                else
                {
                    var def = AppConfig.GetDefaultBackgroundPath();
                    if (File.Exists(def)) p = def;
                }

                if (p == null) { PreviewImage.Source = null; return; }
                PreviewImage.Source = new BitmapImage(new Uri(p));
            }
            catch { PreviewImage.Source = null; }
        }

        // ==================== 教程 ====================

        private async void ResetTutorials_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var c = AppConfig.Load();
                if (c.LockTutorials) return;

                var dlg = new ContentDialog
                {
                    Title = "重看教程",
                    Content = "重置后，下次进入每个页面时会重新显示使用教程。",
                    PrimaryButtonText = "重置",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };
                if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
                TutorialService.ResetAll();
            }
            catch { }
        }

        // ==================== 托盘 ====================

        private void TraySwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            if (TraySwitch == null) return;

            try
            {
                var c = AppConfig.Load();
                if (c.LockTrayIcon)
                {
                    // 被锁定 → 恢复原值
                    _isLoading = true;
                    TraySwitch.IsOn = c.ShowInTray;
                    _isLoading = false;
                    return;
                }

                c.ShowInTray = TraySwitch.IsOn;
                c.Save();
            }
            catch { }
        }

        // ==================== ★ 危险选项（锁定开关）====================

        private void LockShutdown_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            try
            {
                var c = AppConfig.Load();
                c.LockShutdown = LockShutdownSwitch.IsOn;
                c.Save();
            }
            catch { }
        }

        private void LockTrayIcon_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            try
            {
                var c = AppConfig.Load();
                c.LockTrayIcon = LockTrayIconSwitch.IsOn;
                c.Save();

                // 立即应用锁定效果到本页托盘开关
                TraySwitch.IsEnabled = !c.LockTrayIcon;
            }
            catch { }
        }

        private void LockUserInfo_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            try
            {
                var c = AppConfig.Load();
                c.LockUserInfo = LockUserInfoSwitch.IsOn;
                c.Save();

                EditNameButton.IsEnabled = !c.LockUserInfo;
                ChangeAvatarButton.IsEnabled = !c.LockUserInfo;
                UserInfoLockHint.Visibility = c.LockUserInfo ? Visibility.Visible : Visibility.Collapsed;
            }
            catch { }
        }

        private void LockWindowOpacity_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            try
            {
                var c = AppConfig.Load();
                c.LockWindowOpacity = LockWindowOpacitySwitch.IsOn;
                c.Save();
                AlphaSlider.IsEnabled = !c.LockWindowOpacity;
            }
            catch { }
        }

        private void LockBackgroundImage_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            try
            {
                var c = AppConfig.Load();
                c.LockBackgroundImage = LockBackgroundImageSwitch.IsOn;
                c.Save();

                BackgroundSwitch.IsEnabled = !c.LockBackgroundImage;
                ChooseBackgroundButton.IsEnabled = !c.LockBackgroundImage;
                ResetBackgroundButton.IsEnabled = !c.LockBackgroundImage;
            }
            catch { }
        }

        private void LockTutorials_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            try
            {
                var c = AppConfig.Load();
                c.LockTutorials = LockTutorialsSwitch.IsOn;
                c.Save();
                ResetTutorialsButton.IsEnabled = !c.LockTutorials;
            }
            catch { }
        }
    }
}