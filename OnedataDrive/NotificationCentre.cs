using System;
using NLog;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace OnedataDrive
{
    public static class NotificationCentre
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan ReadOnlyNotificationThrottle = TimeSpan.FromMinutes(5);
        private static DateTime? _lastReadOnlyNotificationUtc;
        private static readonly object _lock = new();

        public static void ReadOnlyNotification()
        {
            lock (_lock)
            {
                if (_lastReadOnlyNotificationUtc.HasValue)
                {
                    var elapsed = DateTime.UtcNow - _lastReadOnlyNotificationUtc.Value;
                    if (elapsed < ReadOnlyNotificationThrottle)
                    {
                        // Throttled: do not show notification too frequently
                        return;
                    }
                }
                _lastReadOnlyNotificationUtc = DateTime.UtcNow;
            }

            try
            {
                var appNotification = new AppNotificationBuilder()
                    .SetAppLogoOverride(new Uri("ms-appx:///Assets/Square150x150Logo.png"), AppNotificationImageCrop.Circle)
                    .AddText("Application is running in READ ONLY mode!")
                    .AddText("No changes will be saved to the cloud.")
                    .SetDuration(AppNotificationDuration.Long)
                    .SetScenario(AppNotificationScenario.Reminder)
                    .BuildNotification();

                AppNotificationManager.Default.Show(appNotification);
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "Failed to show ReadOnlyNotification");
            }
        }

        public static void ResetReadOnlyNotificationTimer()
        {
            lock (_lock)
            {
                _lastReadOnlyNotificationUtc = null;
            }
        }
    }
}
