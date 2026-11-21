using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;
using Windows.Graphics;

namespace ShutdownTimer.Views
{
    public sealed partial class OnboardingWindow : Window
    {
        private int _currentStep = 1;
        private const int TotalSteps = 4;

        private string _personalizationType = "Recommended";
        private string _theme = "System";

        public OnboardingWindow()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
            this.AppWindow.Resize(new SizeInt32(800, 620));
            UpdateStep();
        }

        private void UpdateStep()
        {
            Step1.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

            StepIndicator.Text = $"第 {_currentStep} / {TotalSteps} 步";
            BackButton.IsEnabled = _currentStep > 1;
            NextButton.Content = _currentStep == TotalSteps ? "完成" : "下一步";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1) { _currentStep--; UpdateStep(); }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == TotalSteps)
            {
                FinishOnboarding();
                return;
            }

            // 离开第 2 步：保存配置方案
            if (_currentStep == 2)
            {
                if (RecommendedRadio.IsChecked == true) _personalizationType = "Recommended";
                else if (CustomRadio.IsChecked == true) _personalizationType = "Custom";
                else _personalizationType = "Minimal";
            }

            // 离开第 3 步：保存个性化
            if (_currentStep == 3)
            {
                if (LightThemeRadio.IsChecked == true) _theme = "Light";
                else if (DarkThemeRadio.IsChecked == true) _theme = "Dark";
                else _theme = "System";
            }

            _currentStep++;
            UpdateStep();
        }

        private void FinishOnboarding()
        {
            var c = AppConfig.Load();
            c.OnboardingCompleted = true;
            c.PersonalizationType = _personalizationType;
            c.UserName = NameBox.Text.Trim();
            c.AppTheme = _theme;

            // 按方案预设
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
                case "Custom":
                default:
                    // 保持默认
                    break;
            }

            c.Save();

            // 打开主窗口
            var main = new MainWindow();
            main.Activate();

            this.Close();
        }
    }
}