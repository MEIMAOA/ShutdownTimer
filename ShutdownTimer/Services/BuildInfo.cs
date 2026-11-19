namespace ShutdownTimer.Services
{
    /// <summary>编译期信息：区分正式版与教育分支版。</summary>
    public static class BuildInfo
    {
#if EDUCATION
        public static readonly bool IsEducation = true;
        public const string DefaultChannel = "Education";
#else
        public static readonly bool IsEducation = false;
        public const string DefaultChannel = "Official";
#endif
    }
}
