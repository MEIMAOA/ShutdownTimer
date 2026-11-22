using Microsoft.UI.Xaml;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 确保后台承载窗口始终隐藏：不显示、不进任务栏、不进 Alt-Tab。
    /// 即使被系统/外层逻辑意外激活，也会立即再次隐藏。
    /// </summary>
    internal static class HiddenWindowHelper
    {
        public static void EnsureHidden(Window window)
        {
            if (window == null) return;

            Hide(window);

            try
            {
                // 被意外激活（显示）时立刻再隐藏
                window.Activated += (s, e) => Hide(window);

                // 等消息循环起来后再隐藏一次，覆盖初始化时序
                window.DispatcherQueue.TryEnqueue(() => Hide(window));
            }
            catch { }
        }

        private static void Hide(Window window)
        {
            try { window.AppWindow.Hide(); } catch { }
        }
    }
}
