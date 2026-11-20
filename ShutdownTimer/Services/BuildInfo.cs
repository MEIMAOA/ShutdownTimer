namespace ShutdownTimer.Services
{
    /// <summary>编译期信息：区分正式版与教育分支版。</summary>
    public static class BuildInfo
    {
#if EDUCATION
        public static readonly bool IsEducation = true;
        public const string DefaultChannel = "Education";
        public const string AppName = "ShutdownTimer-Edu";
#else
        public static readonly bool IsEducation = false;
        public const string DefaultChannel = "Official";
        public const string AppName = "时序";
#endif
    }
}
