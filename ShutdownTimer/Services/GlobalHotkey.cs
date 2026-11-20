using System;
using System.Runtime.InteropServices;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 全局快捷键（Alt+L）：通过 RegisterHotKey + 子类化窗口过程捕获 WM_HOTKEY。
    /// </summary>
    public static class GlobalHotkey
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 0xB001;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_L = 0x4C;
        private const int GWLP_WNDPROC = -4;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static WndProcDelegate? _proc;
        private static IntPtr _oldProc;
        private static Action? _callback;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        /// <summary>为指定窗口注册 Alt+L；触发时调用 onHotkey。</summary>
        public static bool Register(IntPtr hwnd, Action onHotkey)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return false;

                if (!RegisterHotKey(hwnd, HOTKEY_ID, MOD_ALT | MOD_NOREPEAT, VK_L))
                    return false;

                _callback = onHotkey;
                _proc = WndProc;
                _oldProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_proc));
                return true;
            }
            catch { return false; }
        }

        public static void Unregister(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return;
                UnregisterHotKey(hwnd, HOTKEY_ID);
                if (_oldProc != IntPtr.Zero)
                {
                    SetWindowLongPtr(hwnd, GWLP_WNDPROC, _oldProc);
                    _oldProc = IntPtr.Zero;
                }
            }
            catch { }
        }

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                try { _callback?.Invoke(); } catch { }
                return IntPtr.Zero;
            }

            return CallWindowProc(_oldProc, hWnd, msg, wParam, lParam);
        }
    }
}
