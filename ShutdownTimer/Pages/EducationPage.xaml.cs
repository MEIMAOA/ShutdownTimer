using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    /// <summary>教育分支版专属页面：软件管控（完全禁止 / 每日限时 / 时间段）。</summary>
    public sealed partial class EducationPage : Page
    {
        private List<ManagedApp> _rules = new();
        private bool _loading;

        public EducationPage()
        {
            this.InitializeComponent();
            Load();
        }

        private void Load()
        {
            _loading = true;
            var c = AppConfig.Load();
            _rules = c.ManagedApps != null ? new List<ManagedApp>(c.ManagedApps) : new List<ManagedApp>();
            Render();
            _loading = false;
        }

        private void Render()
        {
            RulesPanel.Children.Clear();

            for (int i = 0; i < _rules.Count; i++)
            {
                int idx = i;
                var rule = _rules[idx];

                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var text = new TextBlock
                {
                    Text = Describe(rule),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)
                        Application.Current.Resources["TextFillColorPrimaryBrush"]
                };
                Grid.SetColumn(text, 0);

                var remove = new Button
                {
                    Content = "移除",
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                remove.Click += (s, e) =>
                {
                    if (idx < _rules.Count) _rules.RemoveAt(idx);
                    Render();
                    Save();
                };
                Grid.SetColumn(remove, 1);

                row.Children.Add(text);
                row.Children.Add(remove);
                RulesPanel.Children.Add(row);
            }

            EmptyHint.Visibility = _rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string Describe(ManagedApp rule)
        {
            string name = rule.Process;

            string mode = rule.MaxMinutesPerDay switch
            {
                0 => "完全禁止",
                > 0 => $"每日限时 {rule.MaxMinutesPerDay} 分钟",
                _ => "不限时长"
            };

            string window = (!string.IsNullOrWhiteSpace(rule.AllowedStart) && !string.IsNullOrWhiteSpace(rule.AllowedEnd))
                ? $"，时间段 {rule.AllowedStart}-{rule.AllowedEnd}"
                : "";

            return $"{name}：{mode}{window}";
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string process = (ProcessNameBox.Text ?? "").Trim();
            if (process.Length == 0) return;

            int mode = GetMode();
            int minutes = 0;
            if (mode == 1)
            {
                minutes = (int)(MinutesBox.Value);
                if (minutes <= 0) minutes = 60;
            }
            else if (mode == -1)
            {
                minutes = -1;
            }

            string start = WindowSwitch.IsOn ? FormatTime(StartTime.Time) : "";
            string end = WindowSwitch.IsOn ? FormatTime(EndTime.Time) : "";

            _rules.Add(new ManagedApp
            {
                Process = process,
                MaxMinutesPerDay = minutes,
                AllowedStart = start,
                AllowedEnd = end
            });

            ProcessNameBox.Text = "";
            Render();
            Save();
        }

        private int GetMode()
        {
            if (ModeBox.SelectedItem is ComboBoxItem item && item.Tag is string s && int.TryParse(s, out int v))
                return v;
            return 0;
        }

        private void WindowSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            WindowPanel.Visibility = WindowSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Save()
        {
            if (_loading) return;
            var c = AppConfig.Load();
            c.ManagedApps = new List<ManagedApp>(_rules);
            c.Save();
        }

        private static string FormatTime(TimeSpan t) => $"{t.Hours:D2}:{t.Minutes:D2}";
    }
}
