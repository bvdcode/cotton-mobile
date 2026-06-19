#if ANDROID
using Android.App;
using Android.Content;
using Android.Util;
using Cotton.Mobile.Services;
using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class AndroidFirebaseMessagingService : FirebaseMessagingService
    {
        private const string LogTag = "CottonPush";

        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);

            ICottonRemotePushTokenRefreshHandler? handler = IPlatformApplication.Current?.Services
                .GetService<ICottonRemotePushTokenRefreshHandler>();
            if (handler is null)
            {
                Log.Info(LogTag, "Skipped Cotton remote push token refresh; handler is unavailable.");
                return;
            }

            _ = HandleNewTokenAsync(handler, token);
        }

        private static async Task HandleNewTokenAsync(
            ICottonRemotePushTokenRefreshHandler handler,
            string token)
        {
            try
            {
                await handler.HandleNewTokenAsync(token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Failed to handle Cotton remote push token refresh. {exception}");
            }
        }
    }
}
#endif
