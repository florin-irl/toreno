using Microsoft.Toolkit.Uwp.Notifications;

namespace Toreno.Notifications;

public static class NotificationService
{
    public static void ShowPlayerJoined(string serverLabel, string playerName)
    {
        new ToastContentBuilder()
            .AddText("Toreno")
            .AddText($"{playerName} just joined {serverLabel}")
            .Show();
    }
}
