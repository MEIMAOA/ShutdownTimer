using System.Diagnostics;

namespace ShutdownTimer.Services
{
    public static class ShellLauncher
    {
        public static void OpenUri(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public static void OpenExe(string exe, string args = "")
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        // ---- 常用系统工具 ----
        public static void OpenSettings() => OpenUri("ms-settings:");
        public static void OpenDisplaySettings() => OpenUri("ms-settings:display");
        public static void OpenNetworkSettings() => OpenUri("ms-settings:network");
        public static void OpenUpdateSettings() => OpenUri("ms-settings:windowsupdate");
        public static void OpenPrivacySettings() => OpenUri("ms-settings:privacy");
        public static void OpenCameraSettings() => OpenUri("ms-settings:privacy-webcam");
        public static void OpenMicrophoneSettings() => OpenUri("ms-settings:privacy-microphone");
        public static void OpenLocationSettings() => OpenUri("ms-settings:privacy-location");
        public static void OpenPowerSettings() => OpenUri("ms-settings:powersleep");
        public static void OpenStorageSettings() => OpenUri("ms-settings:storagesense");
        public static void OpenDefaultApps() => OpenUri("ms-settings:defaultapps");
        public static void OpenStartupApps() => OpenUri("ms-settings:startupapps");

        public static void OpenControlPanel() => OpenExe("control.exe");
        public static void OpenProgramsAndFeatures() => OpenExe("control.exe", "appwiz.cpl");
        public static void OpenDeviceManager() => OpenExe("devmgmt.msc");
        public static void OpenDiskManagement() => OpenExe("diskmgmt.msc");
        public static void OpenServices() => OpenExe("services.msc");
        public static void OpenTaskManager() => OpenExe("taskmgr.exe");
        public static void OpenRegistry() => OpenExe("regedit.exe");
        public static void OpenSystemInfo() => OpenExe("msinfo32.exe");
        public static void OpenCmd() => OpenExe("cmd.exe");
        public static void OpenPowerShell() => OpenExe("powershell.exe");
        public static void OpenNotepad() => OpenExe("notepad.exe");
        public static void OpenCalculator() => OpenExe("calc.exe");
        public static void OpenExplorer() => OpenExe("explorer.exe");
    }
}