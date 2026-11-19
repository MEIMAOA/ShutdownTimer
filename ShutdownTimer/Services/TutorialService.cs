using System.Collections.Generic;

namespace ShutdownTimer.Services
{
    public class TutorialStep
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public static class TutorialService
    {
        private static readonly Dictionary<string, List<TutorialStep>> _tutorials = new()
        {
            ["general"] = new()
            {
                new TutorialStep { Title = "启用时序", Content = "打开右侧开关，程序会在每天指定时间自动关闭电脑。" },
                new TutorialStep { Title = "设置关机时间", Content = "点击时间选择器，选择每天关机的时间点。建议设在睡前。" },
                new TutorialStep { Title = "开机自动启动", Content = "打开「开机自动启动」开关，程序会在每次登录 Windows 后自动运行。" },
                new TutorialStep { Title = "取消关机", Content = "如果关机时间不合适，点「立即取消」按钮即可取消。" },
            },
            ["startup"] = new()
            {
                new TutorialStep { Title = "查看启动项", Content = "这里列出所有开机时自动运行的程序。启动项太多会拖慢开机速度。" },
                new TutorialStep { Title = "禁用或启用", Content = "用右侧的开关控制某项是否开机启动。禁用后仍保留，随时可恢复。" },
                new TutorialStep { Title = "删除启动项", Content = "点「删除」会彻底移除启动项，请谨慎操作。" },
            },
            ["apps"] = new()
            {
                new TutorialStep { Title = "查看已安装应用", Content = "列出电脑上安装的所有应用程序，带图标显示。" },
                new TutorialStep { Title = "搜索应用", Content = "在搜索框输入名称或发布者，快速定位要操作的应用。" },
                new TutorialStep { Title = "卸载应用", Content = "点「卸载」启动系统卸载程序，按提示完成。" },
            },
            ["sysstatus"] = new()
            {
                new TutorialStep { Title = "系统概览", Content = "查看操作系统版本和本次开机时长。" },
                new TutorialStep { Title = "硬件信息", Content = "查看 CPU 型号、核心数和内存使用情况。" },
                new TutorialStep { Title = "磁盘容量", Content = "进度条显示每个分区的使用率，红色表示即将满。" },
            },
            ["cleanup"] = new()
            {
                new TutorialStep { Title = "临时文件清理", Content = "点「立即清理」删除系统临时文件，安全操作不影响使用。" },
                new TutorialStep { Title = "清空回收站", Content = "回收站文件仍占用磁盘，点「清空」彻底释放。" },
                new TutorialStep { Title = "大文件扫描", Content = "扫描磁盘上的大文件，找出占用最多的文件后自行决定。" },
            },
            ["network"] = new()
            {
                new TutorialStep { Title = "网络诊断", Content = "点「开始诊断」测试连通性，全部超时说明网络有问题。" },
                new TutorialStep { Title = "网络适配器", Content = "查看所有网络连接的状态、速度和 MAC 地址。" },
                new TutorialStep { Title = "快捷设置", Content = "一键打开系统网络设置或防火墙。" },
            },
            ["monitoring"] = new()
            {
                new TutorialStep { Title = "进程列表", Content = "显示当前运行的所有程序和它们的内存占用。" },
                new TutorialStep { Title = "搜索进程", Content = "输入名称搜索特定进程。" },
                new TutorialStep { Title = "结束进程", Content = "点「结束进程」强制关闭程序，请谨慎使用。" },
            },
            ["tools"] = new()
            {
                new TutorialStep { Title = "系统设置", Content = "一键打开 Windows 里藏得较深的设置项。" },
                new TutorialStep { Title = "管理工具", Content = "打开控制面板、设备管理器、磁盘管理专业工具。" },
                new TutorialStep { Title = "常用程序", Content = "快速启动任务管理器、CMD、记事本等程序。" },
            },
            ["security"] = new()
            {
                new TutorialStep { Title = "Defender 状态", Content = "查看 Windows 自带杀毒是否开启、病毒库是否最新。" },
                new TutorialStep { Title = "隐私设置", Content = "一键打开摄像头、麦克风、位置权限设置。" },
            },
            ["automation"] = new()
            {
                new TutorialStep { Title = "定时重启", Content = "设置每天自动重启电脑。" },
                new TutorialStep { Title = "定时休眠", Content = "设置每天自动进入休眠，省电保留状态。" },
                new TutorialStep { Title = "定时锁定", Content = "无操作 N 分钟后自动锁屏，保护隐私。" },
            },
            ["appearance"] = new()
            {
                new TutorialStep { Title = "选择主题", Content = "跟随系统、浅色、深色三种主题任你选择。" },
                new TutorialStep { Title = "快捷键", Content = "查看程序支持的快捷键。" },
            },
            ["settings"] = new()
            {
                new TutorialStep { Title = "窗口透明度", Content = "拖动右侧滑块调整窗口透明程度。" },
                new TutorialStep { Title = "显示背景图", Content = "开关背景图。开启时用图片背景，关闭时窗口透明。" },
                new TutorialStep { Title = "系统托盘", Content = "开启后关闭窗口不会退出，从托盘图标重新打开。" },
            },
            ["about"] = new()
            {
                new TutorialStep { Title = "应用信息", Content = "查看版本、版权和许可证信息。" },
                new TutorialStep { Title = "存储位置", Content = "点「打开位置」查看配置文件所在文件夹。" },
            },
        };

        public static List<TutorialStep> GetTutorial(string tag)
            => _tutorials.TryGetValue(tag, out var s) ? s : new List<TutorialStep>();

        public static bool IsCompleted(string tag)
        {
            var c = AppConfig.Load();
            return c.CompletedTutorials.Contains(tag);
        }

        public static void MarkCompleted(string tag)
        {
            var c = AppConfig.Load();
            if (!c.CompletedTutorials.Contains(tag))
            {
                c.CompletedTutorials.Add(tag);
                c.Save();
            }
        }

        /// <summary>重置所有教程（在设置页调用）</summary>
        public static void ResetAll()
        {
            var c = AppConfig.Load();
            c.CompletedTutorials.Clear();
            c.Save();
        }
    }
}