using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Views
{
    public sealed partial class TermsOverlay : UserControl
    {
        /// <summary>用户点击"同意"时触发</summary>
        public event EventHandler? Accepted;

        /// <summary>用户点击"不同意"时触发（一般用于首次启动时退出程序）</summary>
        public event EventHandler? Declined;

        /// <summary>关闭时是否需要显示"不同意"按钮（首次启动时为 true，从关于页查看时为 false）</summary>
        public bool ShowDeclineButton
        {
            get => DeclineButton.Visibility == Visibility.Visible;
            set => DeclineButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        public TermsOverlay()
        {
            this.InitializeComponent();

            TermsText.Text = TermsOfService.GetFullText();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            Accepted?.Invoke(this, EventArgs.Empty);
        }

        private void Decline_Click(object sender, RoutedEventArgs e)
        {
            Declined?.Invoke(this, EventArgs.Empty);
        }
    }
}