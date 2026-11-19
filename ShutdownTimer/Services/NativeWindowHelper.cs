using System;
using System.Runtime.InteropServices;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 封装 Win32 API，用于控制窗口的层级样式和透明度。
    /// </summary>
    public static class NativeWindowHelper
    {
        // Win32 常量
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_ALPHA = 0x2;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        /// <summary>
        /// 为窗口启用分层（Layered）样式，这是使用 SetLayeredWindowAttributes 的前提。
        /// </summary>
        public static bool EnableLayeredWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            // 确保 WS_EX_LAYERED 位被设置
            if ((exStyle & WS_EX_LAYERED) == 0)
            {
                exStyle |= WS_EX_LAYERED;
                var result = SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
                return result != 0;
            }
            return true;
        }

        /// <summary>
        /// 设置窗口的整体不透明度。
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="alpha">不透明度，范围 0 (完全透明) 到 255 (完全不透明)</param>
        public static bool SetWindowOpacity(IntPtr hWnd, byte alpha)
        {
            if (hWnd == IntPtr.Zero) return false;
            // 使用 LWA_ALPHA 标志，忽略 crKey
            return SetLayeredWindowAttributes(hWnd, 0, alpha, LWA_ALPHA);
        }
    }
}