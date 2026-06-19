#if ANDROID
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Services
{
    [Register("dev.cottoncloud.app.AndroidWorkManagerTransferWorker")]
    public sealed class AndroidWorkManagerTransferWorker : Worker
    {
        private const string LogTag = "CottonTransfer";

        public AndroidWorkManagerTransferWorker(
            Context context,
            WorkerParameters workerParameters)
            : base(context, workerParameters)
        {
        }

        public override ListenableWorker.Result DoWork()
        {
            if (!AndroidBackgroundTransferWorkData.TryRead(
                    InputData,
                    out Uri? instanceUri,
                    out Guid transferId,
                    out string displayName,
                    out CottonAndroidTransferWorkKind workKind)
                || instanceUri is null)
            {
                Log.Warn(LogTag, "Android WorkManager transfer worker started without valid Cotton transfer data.");
                return Failure();
            }

            try
            {
                ICottonAndroidBackgroundTransferJobRunner? runner = IPlatformApplication.Current?.Services
                    .GetService<ICottonAndroidBackgroundTransferJobRunner>();
                if (runner is null)
                {
                    Log.Warn(LogTag, "Android WorkManager transfer worker could not resolve Cotton background transfer runner.");
                    return Retry();
                }

                CottonQueuedUploadExecutionResult result = runner
                    .RunAsync(instanceUri, transferId)
                    .GetAwaiter()
                    .GetResult();
                Log.Info(
                    LogTag,
                    $"Android WorkManager transfer worker finished {displayName} ({workKind}) with status {result.Status}.");
                return Success();
            }
            catch (OperationCanceledException)
            {
                return Retry();
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Android WorkManager transfer worker failed before transfer completion. {exception}");
                return Retry();
            }
        }

        private static ListenableWorker.Result Success()
        {
            return ListenableWorker.Result.InvokeSuccess()
                ?? throw new InvalidOperationException("Android WorkManager success result is unavailable.");
        }

        private static ListenableWorker.Result Retry()
        {
            return ListenableWorker.Result.InvokeRetry()
                ?? throw new InvalidOperationException("Android WorkManager retry result is unavailable.");
        }

        private static ListenableWorker.Result Failure()
        {
            return ListenableWorker.Result.InvokeFailure()
                ?? throw new InvalidOperationException("Android WorkManager failure result is unavailable.");
        }
    }
}
#endif
