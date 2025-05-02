using Microsoft.Toolkit.Uwp.Notifications;

namespace VRCXDiscordTracker
{
    /// <summary>
    /// 通知を管理するクラス
    /// </summary>
    internal static class Notifier
    {
        /// <summary>
        /// Windowsのトースト通知を表示する
        /// </summary>
        /// <param name="title">通知のタイトル</param>
        /// <param name="message">通知のメッセージ</param>
        public static void ShowToast(string title, string message)
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
    }
}
