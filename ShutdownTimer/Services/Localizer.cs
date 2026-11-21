using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 遍历视觉树，把 XAML 里硬编码的中文文案按当前语言替换。
    /// 页面在每次导航时重建，因此切换语言后重新进入页面即可生效。
    /// </summary>
    public static class Localizer
    {
        /// <summary>在元素加载完成后再本地化（此时模板/视觉树才完整，ScrollViewer 等模板控件才能遍历到）。</summary>
        public static void ApplyOnLoaded(FrameworkElement? element)
        {
            if (element == null) return;

            if (element.IsLoaded)
            {
                Apply(element);
                return;
            }

            element.Loaded += OnLoaded;
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                fe.Loaded -= OnLoaded;
                Apply(fe);
            }
        }

        public static void Apply(DependencyObject? root)
        {
            if (root == null) return;
            ApplyInternal(root, new HashSet<DependencyObject>());
        }

        /// <summary>同时遍历逻辑树（Content/Children/Child，InitializeComponent 后即完整）与视觉树。</summary>
        private static void ApplyInternal(DependencyObject obj, HashSet<DependencyObject> visited)
        {
            if (obj == null || !visited.Add(obj)) return;

            LocalizeElement(obj);

            switch (obj)
            {
                case ContentControl cc:
                    if (cc.Content is DependencyObject c) ApplyInternal(c, visited);
                    break;
                case Panel panel:
                    foreach (var child in panel.Children) ApplyInternal(child, visited);
                    break;
                case Border border:
                    if (border.Child is DependencyObject bc) ApplyInternal(bc, visited);
                    break;
            }

            int count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                ApplyInternal(VisualTreeHelper.GetChild(obj, i), visited);
            }
        }

        private static void LocalizeElement(DependencyObject obj)
        {
            switch (obj)
            {
                case TextBlock tb:
                    if (tb.Inlines.Count > 0)
                    {
                        foreach (var inline in tb.Inlines)
                        {
                            if (inline is Microsoft.UI.Xaml.Documents.Run run && !string.IsNullOrEmpty(run.Text))
                                run.Text = Loc.T(run.Text);
                        }
                    }
                    else if (!string.IsNullOrEmpty(tb.Text))
                    {
                        tb.Text = Loc.T(tb.Text);
                    }
                    break;

                case TextBox tb:
                    if (!string.IsNullOrEmpty(tb.PlaceholderText)) tb.PlaceholderText = Loc.T(tb.PlaceholderText);
                    break;

                case PasswordBox pb:
                    if (!string.IsNullOrEmpty(pb.PlaceholderText)) pb.PlaceholderText = Loc.T(pb.PlaceholderText);
                    break;

                case Button b:
                    {
                        if (b.Content is string s && s.Length > 0) b.Content = Loc.T(s);
                        break;
                    }

                case HyperlinkButton hb:
                    {
                        if (hb.Content is string s && s.Length > 0) hb.Content = Loc.T(s);
                        break;
                    }

                case CheckBox cb:
                    {
                        if (cb.Content is string s && s.Length > 0) cb.Content = Loc.T(s);
                        break;
                    }

                case RadioButton rb:
                    {
                        if (rb.Content is string s && s.Length > 0) rb.Content = Loc.T(s);
                        break;
                    }

                case ToggleSwitch ts:
                    {
                        if (ts.Header is string h && h.Length > 0) ts.Header = Loc.T(h);
                        if (ts.OnContent is string on && on.Length > 0) ts.OnContent = Loc.T(on);
                        if (ts.OffContent is string off && off.Length > 0) ts.OffContent = Loc.T(off);
                        break;
                    }

                case InfoBar ib:
                    {
                        if (!string.IsNullOrEmpty(ib.Title)) ib.Title = Loc.T(ib.Title);
                        if (ib.Message is string m && m.Length > 0) ib.Message = Loc.T(m);
                        break;
                    }

                case NavigationViewItem nvi:
                    {
                        if (nvi.Content is string s && s.Length > 0) nvi.Content = Loc.T(s);
                        break;
                    }

                case ComboBoxItem cbi:
                    {
                        if (cbi.Content is string s && s.Length > 0) cbi.Content = Loc.T(s);
                        break;
                    }

                case TimePicker tp:
                    Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(tp, Loc.T("时间选择器"));
                    break;

                case MenuFlyoutItem mfi:
                    if (!string.IsNullOrEmpty(mfi.Text)) mfi.Text = Loc.T(mfi.Text);
                    break;
            }
        }
    }
}
