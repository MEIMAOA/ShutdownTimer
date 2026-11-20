using System;

namespace ShutdownTimer.Services
{
    /// <summary>整活弹窗类型。</summary>
    public enum PrankKind
    {
        Education,
        Seewo,
        Chaos
    }

    /// <summary>
    /// 教育分支版「整活」文本源：教育版提示、Seewo 安全提醒、各种胡乱但大致能看懂的文本。
    /// </summary>
    public static class PrankService
    {
        private static readonly string[] EducationMessages =
        {
            "您正在使用教育版",
            "提示：您正在使用教育版",
            "您正在使用教育版！",
            "检测到：您正在使用教育版"
        };

        private static readonly string[] SeewoMessages =
        {
            "保障安全，请不要在 seewo 一体机上登录重要账号",
            "安全提醒：请勿在 seewo 一体机上登录重要账号",
            "为了保障安全，请不要在 seewo 一体机上登录重要账号"
        };

        private static readonly string[] ChaosMessages =
        {
            "检测到您正在认真使用电脑，请继续保持。",
            "温馨提示：您的鼠标好像有点累了。",
            "系统正在努力思考中……请稍候。",
            "请不要关闭本窗口，否则……开玩笑的。",
            "您的注意力刚刚被这个弹窗偷走了 3 秒。",
            "今天的你已经很棒了，真的。",
            "本提示没有实际意义，仅用于陪伴。",
            "如果感到无聊，可以看看窗外。",
            "别忘了喝水。",
            "这是一个随机出现的文本，不代表任何立场。",
            "叮！您有一条提醒：本条提醒没有内容。",
            "系统提示：本系统提示是随机生成的。",
            "正在为你加载一点点好运……加载完成。",
            "你的键盘刚刚叹了口气，可能是太累了。",
            "检测到屏幕前有一位帅哥/美女，请保持微笑。"
        };

        private static readonly Random Rng = new();

        /// <summary>随机取一条整活内容。</summary>
        public static (PrankKind kind, string text) Next()
        {
            int r = Rng.Next(100);

            if (r < 38) return (PrankKind.Education, Pick(EducationMessages));
            if (r < 70) return (PrankKind.Seewo, Pick(SeewoMessages));
            return (PrankKind.Chaos, Pick(ChaosMessages));
        }

        private static string Pick(string[] arr) => arr[Rng.Next(arr.Length)];
    }
}
