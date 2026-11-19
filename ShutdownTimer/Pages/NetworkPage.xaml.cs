using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class NetworkPage : Page
    {
        private readonly ObservableCollection<AdapterInfo> _adapters = new();
        private readonly ObservableCollection<PingResult> _pings = new();

        public NetworkPage()
        {
            this.InitializeComponent();
            AdapterHost.ItemsSource = _adapters;
            DiagHost.ItemsSource = _pings;

            foreach (var a in NetworkService.GetAdapters())
                _adapters.Add(a);
        }

        private async void Diagnose_Click(object sender, RoutedEventArgs e)
        {
            _pings.Clear();
            DiagStatus.Text = "诊断中…";

            var results = await NetworkService.RunDiagnosticsAsync();
            foreach (var r in results) _pings.Add(r);

            DiagStatus.Text = $"诊断完成（{System.DateTime.Now:HH:mm:ss}）";
        }

        private void OpenNetSettings_Click(object sender, RoutedEventArgs e)
            => ShellLauncher.OpenNetworkSettings();

        private void OpenFirewall_Click(object sender, RoutedEventArgs e)
            => ShellLauncher.OpenExe("control.exe", "firewall.cpl");
    }
}