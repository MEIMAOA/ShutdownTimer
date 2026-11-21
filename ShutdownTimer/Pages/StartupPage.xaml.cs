using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ShutdownTimer.Pages
{
    public sealed partial class StartupPage : Page
    {
        private readonly ObservableCollection<StartupItem> _items = new();
        private bool _isLoading = false;

        public StartupPage()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
            ItemsHost.ItemsSource = _items;
            _ = LoadItemsAsync();
        }

        private async Task LoadItemsAsync()
        {
            _isLoading = true;
            CountText.Text = "加载中…";
            _items.Clear();

            var list = await Task.Run(() => StartupItemManager.GetAll());

            foreach (var item in list)
            {
                if (!string.IsNullOrWhiteSpace(item.IconPath))
                {
                    try
                    {
                        item.Icon = IconHelper.LoadIconFromAnyFile(item.IconPath);
                    }
                    catch
                    {
                        item.Icon = null;
                    }
                }

                _items.Add(item);
            }

            CountText.Text = Loc.T("共") + " " + _items.Count + " " + Loc.T("项");
            _isLoading = false;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadItemsAsync();
        }

        private void ItemToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            if (sender is not ToggleSwitch ts) return;
            if (ts.Tag is not StartupItem item) return;

            StartupItemManager.SetEnabled(item, ts.IsOn);
        }

        private async void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not StartupItem item) return;

            var dialog = new ContentDialog
            {
                Title = Loc.T("删除启动项"),
                Content = $"确定要删除「{item.Name}」吗？此操作会修改注册表或删除启动文件夹中的文件。",
                PrimaryButtonText = Loc.T("删除"),
                CloseButtonText = Loc.T("取消"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            if (StartupItemManager.Delete(item))
                _items.Remove(item);

            CountText.Text = Loc.T("共") + " " + _items.Count + " " + Loc.T("项");
        }
    }
}