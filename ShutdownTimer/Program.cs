using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ShutdownTimer
{
    public static class Program
    {
        // ============================================================
        // Win32 API
        // ============================================================

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // ============================================================
        // 常量
        // ============================================================

        private const uint MB_OK = 0x00000000;
        private const uint MB_YESNO = 0x00000004;
        private const uint MB_ICONQUESTION = 0x00000020;
        private const uint MB_ICONINFORMATION = 0x00000040;
        private const uint MB_ICONERROR = 0x00000010;
        private const uint MB_TOPMOST = 0x00040000;
        private const int IDYES = 6;

        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private const uint FLASHW_ALL = 0x00000003;
        private const uint FLASHW_TIMERNOFG = 0x0000000C;

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        // ============================================================
        // 入口
        // ============================================================

        [STAThread]
        static int Main(string[] args)
        {
            // ★ 全局异常捕获（写日志到桌面）
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    string logPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ShutdownTimer_crash.log");
                    File.WriteAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n" +
                        $"系统版本: {Environment.OSVersion}\n" +
                        $"异常: {e.ExceptionObject}\n\n" +
                        $"堆栈: {e.ExceptionObject}");
                }
                catch { }
            };

            // ★ 系统版本检查（Win10 1809 以下不支持）
            try
            {
                var os = Environment.OSVersion.Version;
                if (os.Major < 10 || (os.Major == 10 && os.Build < 17763))
                {
                    MessageBox(IntPtr.Zero,
                        $"本程序需要 Windows 10 版本 1809（Build 17763）或更高版本。\n\n" +
                        $"当前系统：Windows {os.Major}.{os.Minor} Build {os.Build}\n\n" +
                        "请升级系统后重试。",
                        "系统版本不支持",
                        MB_OK | MB_ICONERROR);
                    return 1;
                }
            }
            catch { }

            bool silent = args.Any(a =>
                a.Equals("/silent", StringComparison.OrdinalIgnoreCase));

            bool guardian = args.Any(a =>
                a.Equals("--guardian", StringComparison.OrdinalIgnoreCase));

            // ★ 守护进程：需要与主进程并存，必须跳过单实例检测，
            //   否则会弹出「定时关机程序已在运行中」并直接退出，导致守护失效。
            if (guardian)
                return RunWinUIApp();

            // 单实例检测（仅主进程）
            using var mutex = new Mutex(true, "ShutdownTimer_SingleInstance_Mutex",
                out bool createdNew);

            if (!createdNew)
            {
                // 已有实例在运行
                if (!silent)
                {
                    int result = MessageBox(IntPtr.Zero,
                        "定时关机程序已在运行中。\n\n是否打开已开启的程序窗口？",
                        "定时关机",
                        MB_YESNO | MB_ICONQUESTION | MB_TOPMOST);

                    if (result == IDYES)
                        ActivateExistingWindow();
                }
                return 0;
            }

            return RunWinUIApp();
        }

        /// <summary>启动 WinUI 应用（主进程与守护进程共用）。</summary>
        private static int RunWinUIApp()
        {
            try
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();

                Microsoft.UI.Xaml.Application.Start((p) =>
                {
                    var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }
            catch (Exception ex)
            {
                // 把启动异常写入桌面日志
                try
                {
                    string logPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ShutdownTimer_startup_crash.log");
                    File.WriteAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n" +
                        $"系统版本: {Environment.OSVersion}\n" +
                        $"启动异常: {ex}");
                }
                catch { }

                MessageBox(IntPtr.Zero,
                    $"程序启动失败：\n\n{ex.Message}\n\n" +
                    "详细日志已保存到桌面：ShutdownTimer_startup_crash.log",
                    "启动错误",
                    MB_OK | MB_ICONERROR);
                return 1;
            }

            return 0;
        }

        // ============================================================
        // 激活已有窗口
        // ============================================================

        private static void ActivateExistingWindow()
        {
            try
            {
                IntPtr hwnd = FindExistingWindow();
                if (hwnd == IntPtr.Zero)
                {
                    MessageBox(IntPtr.Zero,
                        "程序正在运行，但未能找到主窗口。\n\n请通过系统托盘图标打开。",
                        "定时关机",
                        MB_OK | MB_ICONINFORMATION);
                    return;
                }

                ForceActivateWindow(hwnd);
            }
            catch { }
        }

        private static IntPtr FindExistingWindow()
        {
            uint currentPid = (uint)Environment.ProcessId;
            string currentProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(currentProcessName);
                foreach (var proc in processes)
                {
                    try
                    {
                        if ((uint)proc.Id == currentPid) continue;

                        uint targetPid = (uint)proc.Id;
                        IntPtr found = IntPtr.Zero;

                        EnumWindows((hWnd, lParam) =>
                        {
                            GetWindowThreadProcessId(hWnd, out uint windowPid);
                            if (windowPid == targetPid)
                            {
                                if (IsWindowVisible(hWnd))
                                {
                                    found = hWnd;
                                    return false;
                                }
                                if (found == IntPtr.Zero)
                                    found = hWnd;
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (found != IntPtr.Zero)
                            return found;
                    }
                    catch { }
                    finally { proc.Dispose(); }
                }
            }
            catch { }

            return IntPtr.Zero;
        }

        private static void ForceActivateWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return;

            ShowWindow(hwnd, SW_SHOW);

            if (IsIconic(hwnd))
                ShowWindow(hwnd, SW_RESTORE);

            bool ok = SetForegroundWindow(hwnd);

            if (!ok)
            {
                uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
                uint targetThread = GetWindowThreadProcessId(hwnd, out _);
                uint currentThread = GetCurrentThreadId();

                bool attached1 = false;
                bool attached2 = false;

                try
                {
                    if (foregroundThread != 0 && foregroundThread != currentThread)
                        attached1 = AttachThreadInput(currentThread, foregroundThread, true);
                    if (targetThread != 0 && targetThread != currentThread)
                        attached2 = AttachThreadInput(currentThread, targetThread, true);

                    SetForegroundWindow(hwnd);
                }
                finally
                {
                    if (attached2) AttachThreadInput(currentThread, targetThread, false);
                    if (attached1) AttachThreadInput(currentThread, foregroundThread, false);
                }
            }

            SetWindowPos(hwnd, HWND_TOP, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            if (GetForegroundWindow() != hwnd)
            {
                var fi = new FLASHWINFO
                {
                    cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                    hwnd = hwnd,
                    dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                    uCount = 0,
                    dwTimeout = 0
                };
                FlashWindowEx(ref fi);
            }
        }
    }
}