using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class SecurityPage : Page
    {
        public SecurityPage()
        {
            this.InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            RtText.Text = "查询中…";
            SigText.Text = "";

            var s = await SecurityService.GetDefenderStatusAsync();
            RtText.Text = s.RealTimeEnabled ? "已开启" : "未开启";
            SigText.Text = s.SignatureLastUpdated;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private void Camera_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenCameraSettings();
        private void Mic_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenMicrophoneSettings();
        private void Location_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenLocationSettings();
        private void AllPrivacy_Click(object s, RoutedEventArgs e) => ShellLauncher.OpenPrivacySettings();
    }
}