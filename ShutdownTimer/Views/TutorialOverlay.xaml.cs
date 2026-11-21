using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;
using System;
using System.Collections.Generic;
using Windows.Web.Http;

namespace ShutdownTimer.Views
{
    public sealed partial class TutorialOverlay : UserControl
    {
        private List<TutorialStep> _steps = new();
        private int _current = 0;
        private string _tag = "";

        public event EventHandler? Completed;

        public TutorialOverlay()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
        }

        public void Start(string tag)
        {
            _tag = tag;
            _steps = TutorialService.GetTutorial(tag);
            _current = 0;

            if (_steps.Count == 0)
            {
                Completed?.Invoke(this, EventArgs.Empty);
                return;
            }

            ShowStep();
        }

        private void ShowStep()
        {
            if (_current < 0 || _current >= _steps.Count) return;

            var step = _steps[_current];
            StepLabel.Text = $"第 {_current + 1} / {_steps.Count} 步";
            TitleText.Text = step.Title;
            ContentText.Text = step.Content;
            StepProgress.Maximum = _steps.Count;
            StepProgress.Value = _current + 1;

            NextButton.Content = _current == _steps.Count - 1 ? "完成" : "下一步";
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_current == _steps.Count - 1)
            {
                TutorialService.MarkCompleted(_tag);
                Completed?.Invoke(this, EventArgs.Empty);
                return;
            }
            _current++;
            ShowStep();
        }
    }
}