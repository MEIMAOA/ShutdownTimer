using System;

namespace ShutdownTimer.Services
{
    /// <summary>编译期信息：区分正式版与教育分支版。</summary>
    public static class BuildInfo
    {
#if EDUCATION
        public static readonly bool IsEducation = true;
        public const string DefaultChannel = "Education";
        public const string AppName = "ShutdownTimer-Edu";
        public const string VersionSuffix = "-Edu";
#else
        public static readonly bool IsEducation = false;
        public const string DefaultChannel = "Official";
        public const string AppName = "时序";
        public const string VersionSuffix = "";
#endif

        /// <summary>当前程序集版本 + 版本后缀（教育版为 -Edu）。</summary>
        public static string GetDisplayVersion()
        {
            try
            {
                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return (v != null ? "v" + v : "v0.0.0.0") + VersionSuffix;
            }
            catch
            {
                return "v0.0.0.0" + VersionSuffix;
            }
        }
    }
}
