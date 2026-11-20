using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class GeneralPage : Page
    {
        private bool _isLoading = false;
        private List<string> _times = new();

        public GeneralPage()
        {
            this.InitializeComponent();
            LoadConfig();
        }

        private void LoadConfig()
        {
            _isLoading = true;
            try
            {
                var config = AppConfig.Load();
                EnableSwitch.IsOn = config.Enabled;
                _times = config.ShutdownTimes != null
                    ? new List<string>(config.ShutdownTimes)
                    : new List<string>();
                AutoStartSwitch.IsOn = StartupManager.IsEnabled();

                RenderTimeEntries();
                ApplyLockState(config);
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>根据时间段列表动态生成「星期 + 时间 + 删除」行。</summary>
        private void RenderTimeEntries()
        {
            TimesPanel.Children.Clear();

            for (int i = 0; i < _times.Count; i++)
            {
                int idx = i;
                var (day, time) = ParseEntry(_times[idx]);

                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var dayBox = CreateDayComboBox(day);
                Grid.SetColumn(dayBox, 0);

                var picker = new TimePicker
                {
                    ClockIdentifier = "24HourClock",
                    MinuteIncrement = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                picker.Time = time;
                Grid.SetColumn(picker, 1);

                var remove = new Button
                {
                    Content = "删除",
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(remove, 2);

                dayBox.SelectionChanged += (s, e) =>
                {
                    if (_isLoading) return;
                    _times[idx] = FormatEntry(GetDay(dayBox), picker.Time);
                    SaveConfig();
                };
                picker.SelectedTimeChanged += (s, e) =>
                {
                    if (_isLoading) return;
                    _times[idx] = FormatEntry(GetDay(dayBox), picker.Time);
                    SaveConfig();
                };
                remove.Click += (s, e) =>
                {
                    if (_isLoading) return;
                    if (idx < _times.Count) _times.RemoveAt(idx);
                    RenderTimeEntries();
                    SaveConfig();
                };

                row.Children.Add(dayBox);
                row.Children.Add(picker);
                row.Children.Add(remove);
                TimesPanel.Children.Add(row);
            }
        }

        /// <summary>
        /// 根据锁定状态禁用所有控件
        /// </summary>
        private void ApplyLockState(AppConfig config)
        {
            try
            {
                bool locked = config.LockShutdown;

                EnableSwitch.IsEnabled = !locked;
                AutoStartSwitch.IsEnabled = !locked;
                AddTimeButton.IsEnabled = !locked;

                foreach (var child in TimesPanel.Children)
                {
                    if (child is Grid row)
                    {
                        foreach (var c in row.Children)
                        {
                            if (c is Control control) control.IsEnabled = !locked;
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            if (_isLoading) return;

            // 二次保险：被锁定时不允许保存
            var check = AppConfig.Load();
            if (check.LockShutdown) return;

            var config = AppConfig.Load();
            config.Enabled = EnableSwitch.IsOn;
            config.ShutdownTimes = new List<string>(_times);
            config.Save();
        }

        private void AddTime_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            _times.Add(NextDefaultTime());
            RenderTimeEntries();
            SaveConfig();
        }

        /// <summary>新加时间段的默认值：取当前时间向后取整的整点，避免重复。</summary>
        private string NextDefaultTime()
        {
            var now = DateTime.Now;
            int h = (now.Hour + 1) % 24;
            string candidate = $"{h:D2}:00";

            int guard = 0;
            while (_times.Contains(candidate) && guard < 24)
            {
                h = (h + 1) % 24;
                candidate = $"{h:D2}:00";
                guard++;
            }
            return candidate;
        }

        private void EnableSwitch_Toggled(object sender, RoutedEventArgs e)
            => SaveConfig();

        private void AutoStartSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            var check = AppConfig.Load();
            if (check.LockShutdown)
            {
                _isLoading = true;
                AutoStartSwitch.IsOn = StartupManager.IsEnabled();
                _isLoading = false;
                return;
            }

            if (AutoStartSwitch.IsOn)
                StartupManager.Enable();
            else
                StartupManager.Disable();
        }

        private void CancelShutdownButton_Click(object sender, RoutedEventArgs e)
        {
            var check = AppConfig.Load();
            if (check.LockShutdown) return;

            var config = AppConfig.Load();
            config.Enabled = false;
            config.Save();

            _isLoading = true;
            EnableSwitch.IsOn = false;
            _isLoading = false;
        }

        // ==================== 星期 / 时间解析 ====================

        private static ComboBox CreateDayComboBox(int? day)
        {
            var cb = new ComboBox
            {
                MinWidth = 96,
                VerticalAlignment = VerticalAlignment.Center
            };
            cb.Items.Add(new ComboBoxItem { Content = "每天", Tag = -1 });

            string[] names = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            for (int d = 0; d <= 6; d++)
                cb.Items.Add(new ComboBoxItem { Content = names[d], Tag = d });

            cb.SelectedIndex = day.HasValue ? day.Value + 1 : 0;
            return cb;
        }

        private static int? GetDay(ComboBox cb)
        {
            if (cb.SelectedItem is ComboBoxItem item && item.Tag is int d)
                return d == -1 ? null : d;
            return null;
        }

        /// <summary>解析 "HH:mm"（每天）或 "D:HH:mm"（D=0..6，周日=0）。</summary>
        private static (int? day, TimeSpan time) ParseEntry(string s)
        {
            var parts = s.Split(':');
            if (parts.Length == 3 && int.TryParse(parts[0], out int d) && d >= 0 && d <= 6)
            {
                int h = parts.Length > 1 && int.TryParse(parts[1], out var hh) ? hh : 0;
                int m = parts.Length > 2 && int.TryParse(parts[2], out var mm) ? mm : 0;
                return (d, new TimeSpan(Math.Clamp(h, 0, 23), Math.Clamp(m, 0, 59), 0));
            }
            return (null, ParseTime(s));
        }

        private static string FormatEntry(int? day, TimeSpan t)
        {
            string tm = FormatTime(t);
            return day.HasValue ? $"{day.Value}:{tm}" : tm;
        }

        private static TimeSpan ParseTime(string s)
        {
            var parts = s.Split(':');
            int h = parts.Length > 0 && int.TryParse(parts[0], out var hh) ? hh : 0;
            int m = parts.Length > 1 && int.TryParse(parts[1], out var mm) ? mm : 0;
            return new TimeSpan(Math.Clamp(h, 0, 23), Math.Clamp(m, 0, 59), 0);
        }

        private static string FormatTime(TimeSpan t) => $"{t.Hours:D2}:{t.Minutes:D2}";
    }
}
