#if ANDROID
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Services
{
    [Register("dev.cottoncloud.app.AndroidWorkManagerSyncWorker")]
    public class AndroidWorkManagerSyncWorker : Worker
    {
        private const string LogTag = "CottonSync";

        public AndroidWorkManagerSyncWorker(
            Context context,
            WorkerParameters workerParameters)
            : base(context, workerParameters)
        {
        }

        public override ListenableWorker.Result DoWork()
        {
            if (!AndroidBackgroundSyncWorkData.TryRead(
                    InputData,
                    out Uri? instanceUri,
                    out int eligibleRootCount)
                || instanceUri is null)
            {
                Log.Warn(LogTag, "Android WorkManager sync worker started without valid Cotton sync data.");
                return Failure();
            }

            try
            {
                ICottonAndroidBackgroundSyncJobRunner? runner = IPlatformApplication.Current?.Services
                    .GetService<ICottonAndroidBackgroundSyncJobRunner>();
                if (runner is null)
                {
                    Log.Warn(LogTag, "Android WorkManager sync worker could not resolve Cotton background sync runner.");
                    return Retry();
                }

                CottonCloudToDeviceSyncRunSummary summary = runner
                    .RunAsync(instanceUri)
                    .GetAwaiter()
                    .GetResult();
                Log.Info(
                    LogTag,
                    $"Android WorkManager sync worker finished {eligibleRootCount} eligible roots with {summary.CompletedRootCount} completed, {summary.SkippedRootCount} skipped, {summary.DownloadedCount} downloaded, {summary.RefreshedCount} refreshed.");
                return Success();
            }
            catch (OperationCanceledException)
            {
                return Retry();
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Android WorkManager sync worker failed before sync completion. {exception}");
                return Retry();
            }
        }

        private static ListenableWorker.Result Success()
        {
            return ListenableWorker.Result.InvokeSuccess()
                ?? throw new InvalidOperationException("Android WorkManager sync success result is unavailable.");
        }

        private static ListenableWorker.Result Retry()
        {
            return ListenableWorker.Result.InvokeRetry()
                ?? throw new InvalidOperationException("Android WorkManager sync retry result is unavailable.");
        }

        private static ListenableWorker.Result Failure()
        {
            return ListenableWorker.Result.InvokeFailure()
                ?? throw new InvalidOperationException("Android WorkManager sync failure result is unavailable.");
        }
    }
}
#endif
