using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using ShutdownTimer.Services;
using Windows.Storage.Pickers;

namespace ShutdownTimer.Views
{
    public sealed partial class OnboardingOverlay : UserControl
    {
        private class Step
        {
            public string Title = "";
            public string Content = "";
            public StepKind Kind = StepKind.Info;
        }

        private enum StepKind
        {
            Info,
            ConfigChoice,
            Personalize,
            Finish
        }

        private readonly List<Step> _steps = new();
        private int _current = 0;

        private string _personalizationType = "Recommended";
        private string _theme = "System";
        private string _avatarPath = "";

        public event EventHandler? Completed;

        public OnboardingOverlay()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
            BuildSteps();
            ShowStep();
        }

        private void BuildSteps()
        {
            _steps.Add(new Step
            {
                Title = "👋 欢迎使用时序",
                Content = "一个帮你管理电脑、让作息更规律的小助手。\n\n" +
                          "接下来我们会向你介绍全部功能亮点，请逐步点击「下一步」查看。\n" +
                          "（不能跳过，看完即完成初始化）"
            });

            _steps.Add(new Step
            {
                Title = "💡 亮点一：定时关机",
                Content = "• 设置每天固定时间自动关机\n" +
                          "• 支持开机自启、系统托盘静默运行\n" +
                          "• 到点前系统会提示，可随时取消\n\n" +
                          "适合用来约束自己按时休息，或让电脑定时省电。"
            });

            _steps.Add(new Step
            {
                Title = "💡 亮点二：系统状态 & 清理优化",
                Content = "• 系统状态：一眼查看 CPU / 内存 / 磁盘占用\n" +
                          "• 清理优化：一键清理临时文件、清空回收站\n" +
                          "• 大文件扫描：快速定位占用磁盘最多的文件\n\n" +
                          "让你的电脑始终保持清爽。"
            });

            _steps.Add(new Step
            {
                Title = "💡 亮点三：网络连接 & 系统监控",
                Content = "• 网络诊断：一键 ping 网关、DNS，快速定位断网问题\n" +
                          "• 网卡信息：查看所有网络适配器和连接状态\n" +
                          "• 进程管理：查看运行中的程序，结束卡死进程\n\n" +
                          "替代系统里藏得很深的工具。"
            });

            _steps.Add(new Step
            {
                Title = "💡 亮点四：常用工具 & 安全隐私",
                Content = "• 一键打开系统设置、控制面板、设备管理器\n" +
                          "• 启动项管理：禁用拖慢开机的自启程序\n" +
                          "• 应用管理：查看并卸载已安装的应用\n" +
                          "• Defender 状态检查、隐私权限快捷入口\n\n" +
                          "让新手也能轻松管理电脑。"
            });

            _steps.Add(new Step
            {
                Title = "💡 亮点五：自动化 & 外观体验",
                Content = "• 定时重启、定时休眠、无操作自动锁屏\n" +
                          "• 深色 / 浅色 / 跟随系统三种主题\n" +
                          "• 透明窗口 + 背景图自由切换\n\n" +
                          "让电脑按你的习惯运行。"
            });

            _steps.Add(new Step
            {
                Title = "⚙️ 选择配置方案",
                Content = "根据你的使用习惯选择一套预设，稍后随时可以修改。",
                Kind = StepKind.ConfigChoice
            });

            _steps.Add(new Step
            {
                Title = "🎨 个性化",
                Content = "请填写你的称呼并选择头像，这两项为必填内容，将显示在窗口左上角。",
                Kind = StepKind.Personalize
            });

            _steps.Add(new Step
            {
                Title = "🎉 全部就绪！",
                Content = "配置已保存。每个页面的首次进入都会显示使用教程，\n" +
                          "逐步引导你熟悉功能。\n\n" +
                          "点击「完成」开始使用。",
                Kind = StepKind.Finish
            });
        }

        private void ShowStep()
        {
            if (_current < 0 || _current >= _steps.Count) return;
            var step = _steps[_current];

            StepLabel.Text = $"第 {_current + 1} / {_steps.Count} 步";
            TitleText.Text = step.Title;
            ContentText.Text = step.Content;

            ConfigOptions.Visibility =
                step.Kind == StepKind.ConfigChoice ? Visibility.Visible : Visibility.Collapsed;

            PersonalOptions.Visibility =
                step.Kind == StepKind.Personalize ? Visibility.Visible : Visibility.Collapsed;

            StepProgress.Maximum = _steps.Count;
            StepProgress.Value = _current + 1;

            NextButton.Content = _current == _steps.Count - 1 ? "完成" : "下一步";

            // 个性化步骤时隐藏错误提示
            if (step.Kind == StepKind.Personalize)
                NameError.Visibility = Visibility.Collapsed;
        }

        // ==================== 头像选择 ====================

        private async void ChooseAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                _avatarPath = file.Path;

                // 预览
                try
                {
                    var bitmap = new BitmapImage(new Uri(_avatarPath));
                    AvatarPreview.Source = bitmap;
                    AvatarPreview.Visibility = Visibility.Visible;
                    AvatarPlaceholder.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    AvatarPreview.Visibility = Visibility.Collapsed;
                    AvatarPlaceholder.Visibility = Visibility.Visible;
                }

                // 清除错误提示
                NameError.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        // ==================== 下一步 ====================

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            // 离开配置方案步骤 → 保存选择
            if (_steps[_current].Kind == StepKind.ConfigChoice)
            {
                if (RecommendedRadio.IsChecked == true) _personalizationType = "Recommended";
                else if (CustomRadio.IsChecked == true) _personalizationType = "Custom";
                else _personalizationType = "Minimal";
            }

            // 离开个性化步骤 → 校验必填
            if (_steps[_current].Kind == StepKind.Personalize)
            {
                // ★ 用户名必填
                string name = NameBox.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    NameError.Text = "请填写你的称呼（必填）";
                    NameError.Visibility = Visibility.Visible;
                    NameBox.Focus(FocusState.Programmatic);
                    return;
                }

                // ★ 头像必填
                if (string.IsNullOrEmpty(_avatarPath) || !File.Exists(_avatarPath))
                {
                    NameError.Text = "请选择头像（必填）";
                    NameError.Visibility = Visibility.Visible;
                    return;
                }

                // 保存主题
                if (LightThemeRadio.IsChecked == true) _theme = "Light";
                else if (DarkThemeRadio.IsChecked == true) _theme = "Dark";
                else _theme = "System";
            }

            // 最后一步 → 完成
            if (_current == _steps.Count - 1)
            {
                Finish();
                return;
            }

            _current++;
            ShowStep();
        }

        private void Finish()
        {
            var c = AppConfig.Load();
            c.OnboardingCompleted = true;
            c.PersonalizationType = _personalizationType;
            c.UserName = NameBox.Text.Trim();
            c.AvatarPath = _avatarPath;
            c.AppTheme = _theme;

            switch (_personalizationType)
            {
                case "Recommended":
                    c.Enabled = true;
                    c.ShutdownHour = 23;
                    c.ShutdownMinute = 0;
                    c.ShowInTray = true;
                    c.ShowBackgroundImage = true;
                    break;
                case "Minimal":
                    c.Enabled = false;
                    c.ShowInTray = false;
                    c.ShowBackgroundImage = false;
                    break;
                default:
                    break;
            }
            c.Save();

            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}