using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class SystemStatusPage : Page
    {
        private readonly List<DiskInfo> _disks = new();

        public SystemStatusPage()
        {
            this.InitializeComponent();
            DiskHost.ItemsSource = _disks;
            Refresh();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

        private void Refresh()
        {
            try
            {
                OsText.Text = SystemInfoService.GetOsName();
                UptimeText.Text = SystemInfoService.FormatUptime(SystemInfoService.GetUptime());
                CpuText.Text = SystemInfoService.GetCpuName();
                CoresText.Text = $"{SystemInfoService.GetCpuCores()} 线程";

                var mem = SystemInfoService.GetMemory();
                MemoryText.Text = $"{SystemInfoService.FormatBytes(mem.totalBytes - mem.availBytes)} / " +
                                  $"{SystemInfoService.FormatBytes(mem.totalBytes)}  ({mem.usedPercent:0}%)";

                _disks.Clear();
                foreach (var d in SystemInfoService.GetDisks())
                    _disks.Add(d);

                UpdatedText.Text = $"更新于 {DateTime.Now:HH:mm:ss}";
            }
            catch { }
        }
    }
}