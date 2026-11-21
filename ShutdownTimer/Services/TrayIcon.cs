using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 用 Shell_NotifyIcon API 实现的托盘图标，无第三方依赖。
    /// 内部启动独立线程跑消息循环。
    /// </summary>
    public sealed class TrayIcon : IDisposable
    {
        // ==== 常量 ====
        private const int WM_APP = 0x8000;
        private const int WM_TRAY = WM_APP + 1;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;
        private const int NIM_ADD = 0x00000000;
        private const int NIM_DELETE = 0x00000002;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int TRAY_ID = 1;

        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint CMD_SHOW = 1001;
        private const uint CMD_EXIT = 1002;

        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        // ==== Win32 结构体 ====
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSEX
        {
            public int cbSize;
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        // ==== Win32 API ====
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int w, int h, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y,
            int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        private const int IDI_APPLICATION = 32512;

        // ==== 字段 ====
        private Thread? _thread;
        private IntPtr _hwnd = IntPtr.Zero;
        private IntPtr _hMenu = IntPtr.Zero;
        private NOTIFYICONDATA _data;
        private readonly ManualResetEventSlim _ready = new(false);
        private readonly WndProcDelegate _wndProcDelegate;
        private bool _disposed;

        // ==== 事件（在 UI 线程触发）====
        public event Action? LeftClick;
        public event Action? ShowRequested;
        public event Action? ExitRequested;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public TrayIcon(string tooltip = "时序")
        {
            _wndProcDelegate = WndProc;

            _thread = new Thread(ThreadProc)
            {
                IsBackground = true,
                Name = "TrayIconMessageLoop"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();

            _ready.Wait(); // 等窗口创建完成再返回

            _data = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = TRAY_ID,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAY,
                hIcon = LoadIcon(IntPtr.Zero, new IntPtr(IDI_APPLICATION)),
                szTip = tooltip
            };
            Shell_NotifyIcon(NIM_ADD, ref _data);
        }

        private void ThreadProc()
        {
            var wc = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                hInstance = System.Diagnostics.Process.GetCurrentProcess().Handle,
                lpszClassName = "ShutdownTimerTrayWnd_" + Guid.NewGuid().ToString("N")
            };
            RegisterClassEx(ref wc);

            _hwnd = CreateWindowEx(0, wc.lpszClassName, "", 0, 0, 0, 0, 0,
                HWND_MESSAGE, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

            // 右键菜单
            _hMenu = CreatePopupMenu();
            AppendMenu(_hMenu, MF_STRING, CMD_SHOW, Loc.T("显示主窗口"));
            AppendMenu(_hMenu, MF_SEPARATOR, 0, "");
            AppendMenu(_hMenu, MF_STRING, CMD_EXIT, Loc.T("退出"));

            _ready.Set();

            // 消息循环
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAY)
            {
                int mouseMsg = lParam.ToInt32() & 0xFFFF;

                if (mouseMsg == WM_LBUTTONUP)
                {
                    LeftClick?.Invoke();
                }
                else if (mouseMsg == WM_RBUTTONUP)
                {
                    GetCursorPos(out var pt);
                    SetForegroundWindow(hWnd);
                    uint cmd = TrackPopupMenu(_hMenu,
                        TPM_RETURNCMD | TPM_RIGHTBUTTON,
                        pt.X, pt.Y, 0, hWnd, IntPtr.Zero);
                    if (cmd == CMD_SHOW) ShowRequested?.Invoke();
                    else if (cmd == CMD_EXIT) ExitRequested?.Invoke();
                    PostMessage(hWnd, 0, IntPtr.Zero, IntPtr.Zero);
                }
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                Shell_NotifyIcon(NIM_DELETE, ref _data);
                if (_hMenu != IntPtr.Zero) DestroyMenu(_hMenu);
                if (_hwnd != IntPtr.Zero) PostMessage(_hwnd, 0x0010 /*WM_CLOSE*/, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }
    }
}