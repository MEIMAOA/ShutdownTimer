using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class AppearancePage : Page
    {
        private bool _loading = false;

        public AppearancePage()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
            _loading = true;

            var c = AppConfig.Load();
            switch (c.AppTheme)
            {
                case "Light": ThemeLight.IsChecked = true; break;
                case "Dark": ThemeDark.IsChecked = true; break;
                default: ThemeSystem.IsChecked = true; break;
            }

            _loading = false;
        }

        private void Theme_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            string theme = "System";
            if (ThemeLight.IsChecked == true) theme = "Light";
            else if (ThemeDark.IsChecked == true) theme = "Dark";

            var c = AppConfig.Load();
            c.AppTheme = theme;
            c.Save();

            ApplyTheme(theme);
        }

        /// <summary>切换根元素的 RequestedTheme 实现全局主题</summary>
        public static void ApplyTheme(string theme)
        {
            if (MainWindow.MainWindowInstance?.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }
    }
}