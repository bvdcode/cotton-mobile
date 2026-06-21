#if ANDROID
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Services
{
    [Register("dev.cottoncloud.app.AndroidRemotePushTokenRefreshWorker")]
    public class AndroidRemotePushTokenRefreshWorker : Worker
    {
        private const string LogTag = "CottonRemotePush";

        public AndroidRemotePushTokenRefreshWorker(
            Context context,
            WorkerParameters workerParameters)
            : base(context, workerParameters)
        {
        }

        public override ListenableWorker.Result DoWork()
        {
            try
            {
                ICottonRemotePushSessionRegistrationService? registrationService =
                    IPlatformApplication.Current?.Services
                        .GetService<ICottonRemotePushSessionRegistrationService>();
                if (registrationService is null)
                {
                    Log.Warn(LogTag, "Android remote push token refresh worker could not resolve registration service.");
                    return Retry();
                }

                registrationService
                    .RefreshCurrentSessionBestEffortAsync()
                    .GetAwaiter()
                    .GetResult();
                Log.Info(LogTag, "Android remote push token refresh worker finished.");
                return Success();
            }
            catch (OperationCanceledException)
            {
                return Retry();
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Android remote push token refresh worker failed before completion. {exception}");
                return Retry();
            }
        }

        private static ListenableWorker.Result Success()
        {
            return ListenableWorker.Result.InvokeSuccess()
                ?? throw new InvalidOperationException("Android WorkManager remote push token refresh success result is unavailable.");
        }

        private static ListenableWorker.Result Retry()
        {
            return ListenableWorker.Result.InvokeRetry()
                ?? throw new InvalidOperationException("Android WorkManager remote push token refresh retry result is unavailable.");
        }
    }
}
#endif
