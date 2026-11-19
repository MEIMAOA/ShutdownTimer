using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    /// <summary>教育分支版专属页面：管理"禁止打开的软件"黑名单。</summary>
    public sealed partial class EducationPage : Page
    {
        private List<string> _blocked = new();

        public EducationPage()
        {
            this.InitializeComponent();
            Load();
        }

        private void Load()
        {
            var c = AppConfig.Load();
            _blocked = c.BlockedApps != null
                ? new List<string>(c.BlockedApps)
                : new List<string>();
            Render();
        }

        private void Render()
        {
            BlockedListPanel.Children.Clear();

            for (int i = 0; i < _blocked.Count; i++)
            {
                int idx = i;

                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var nameText = new TextBlock
                {
                    Text = _blocked[idx],
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)
                        Application.Current.Resources["TextFillColorPrimaryBrush"]
                };
                Grid.SetColumn(nameText, 0);

                var remove = new Button
                {
                    Content = "移除",
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                remove.Click += (s, e) =>
                {
                    if (idx < _blocked.Count) _blocked.RemoveAt(idx);
                    Render();
                    Save();
                };
                Grid.SetColumn(remove, 1);

                row.Children.Add(nameText);
                row.Children.Add(remove);
                BlockedListPanel.Children.Add(row);
            }

            EmptyHint.Visibility = _blocked.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string name = (ProcessNameBox.Text ?? "").Trim();
            if (name.Length == 0) return;

            // 去掉可能输入的 .exe 后缀，进程名不含扩展名
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            if (!_blocked.Contains(name)) _blocked.Add(name);
            ProcessNameBox.Text = "";
            Render();
            Save();
        }

        private void Save()
        {
            var c = AppConfig.Load();
            c.BlockedApps = new List<string>(_blocked);
            c.Save();
        }
    }
}
