using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class AutomationPage : Page
    {
        private bool _loading = false;

        public AutomationPage()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
            Load();
        }

        private void Load()
        {
            _loading = true;
            try
            {
                var c = AppConfig.Load();
                RestartSwitch.IsOn = c.AutoRestartEnabled;
                RestartTime.Time = new TimeSpan(c.AutoRestartHour, c.AutoRestartMinute, 0);
                HibernateSwitch.IsOn = c.AutoHibernateEnabled;
                HibernateTime.Time = new TimeSpan(c.AutoHibernateHour, c.AutoHibernateMinute, 0);
                LockMinutesBox.Value = c.AutoLockMinutes;
            }
            finally { _loading = false; }
        }

        private void SaveConfig()
        {
            if (_loading) return;
            var c = AppConfig.Load();
            c.AutoRestartEnabled = RestartSwitch.IsOn;
            c.AutoRestartHour = RestartTime.Time.Hours;
            c.AutoRestartMinute = RestartTime.Time.Minutes;
            c.AutoHibernateEnabled = HibernateSwitch.IsOn;
            c.AutoHibernateHour = HibernateTime.Time.Hours;
            c.AutoHibernateMinute = HibernateTime.Time.Minutes;
            c.AutoLockMinutes = (int)(LockMinutesBox.Value);
            c.Save();
        }

        private void RestartSwitch_Toggled(object s, RoutedEventArgs e) => SaveConfig();
        private void RestartTime_Changed(TimePicker s, TimePickerSelectedValueChangedEventArgs a) => SaveConfig();
        private void HibernateSwitch_Toggled(object s, RoutedEventArgs e) => SaveConfig();
        private void HibernateTime_Changed(TimePicker s, TimePickerSelectedValueChangedEventArgs a) => SaveConfig();
        private void LockMinutes_Changed(NumberBox s, NumberBoxValueChangedEventArgs a) => SaveConfig();

        private void TestLock_Click(object sender, RoutedEventArgs e)
        {
            ShutdownHelper.Lock();
        }

        private void TestHibernate_Click(object sender, RoutedEventArgs e)
        {
            ShutdownHelper.Hibernate();
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            // 自定时模式下没有系统级待取消任务，这里关闭重启与休眠计划
            _loading = true;
            RestartSwitch.IsOn = false;
            HibernateSwitch.IsOn = false;
            _loading = false;

            var c = AppConfig.Load();
            c.AutoRestartEnabled = false;
            c.AutoHibernateEnabled = false;
            c.Save();
        }
    }
}