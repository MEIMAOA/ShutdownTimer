using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class ToolsPage : Page
    {
        public ToolsPage()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
        }

        // 系统设置
        private void Display_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenDisplaySettings();
        private void Network_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenNetworkSettings();
        private void Power_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenPowerSettings();
        private void Storage_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenStorageSettings();
        private void Update_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenUpdateSettings();
        private void DefaultApps_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenDefaultApps();
        private void StartupApps_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenStartupApps();

        // 管理工具
        private void CtrlPanel_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenControlPanel();
        private void ProgFeat_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenProgramsAndFeatures();
        private void DevMgr_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenDeviceManager();
        private void DiskMgr_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenDiskManagement();
        private void Services_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenServices();
        private void SysInfo_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenSystemInfo();
        private void Regedit_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenRegistry();

        // 常用程序
        private void TaskMgr_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenTaskManager();
        private void Cmd_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenCmd();
        private void Pwsh_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenPowerShell();
        private void Notepad_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenNotepad();
        private void Calc_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenCalculator();
        private void Explorer_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenExplorer();
    }
}