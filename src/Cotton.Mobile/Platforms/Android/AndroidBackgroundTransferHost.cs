#if ANDROID
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using Android.Util;

namespace Cotton.Mobile.Services
{
    public sealed class AndroidBackgroundTransferHost : ICottonAndroidBackgroundTransferHost
    {
        private const string LogTag = "CottonTransfer";
        private const int AndroidUserInitiatedDataTransferApiLevel = 34;

        public Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundTransferRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            return request.Host switch
            {
                CottonAndroidTransferExecutionHost.UserInitiatedDataTransfer =>
                    Task.FromResult(ScheduleUserInitiatedDataTransfer(request)),
                CottonAndroidTransferExecutionHost.WorkManagerConstrained =>
                    Task.FromResult(CottonAndroidBackgroundTransferScheduleResult.Unsupported(
                        request,
                        "Android WorkManager transfer host is not wired yet.")),
                _ => Task.FromResult(CottonAndroidBackgroundTransferScheduleResult.ForegroundRequired(
                    request,
                    request.Strategy.StatusText)),
            };
        }

        private static CottonAndroidBackgroundTransferScheduleResult ScheduleUserInitiatedDataTransfer(
            CottonAndroidBackgroundTransferRequest request)
        {
            if ((int)Build.VERSION.SdkInt < AndroidUserInitiatedDataTransferApiLevel)
            {
                return CottonAndroidBackgroundTransferScheduleResult.ForegroundRequired(
                    request,
                    request.Strategy.StatusText);
            }

            Context context = Android.App.Application.Context;
            if (context.GetSystemService(Context.JobSchedulerService) is not JobScheduler jobScheduler)
            {
                return CottonAndroidBackgroundTransferScheduleResult.Unsupported(
                    request,
                    "Android JobScheduler is unavailable.");
            }

            JobInfo jobInfo = CreateJobInfo(context, request);
            int scheduleResult = jobScheduler.Schedule(jobInfo);
            if (scheduleResult != JobScheduler.ResultSuccess)
            {
                Log.Warn(LogTag, $"Android UIDT scheduling failed for transfer {request.TransferId}.");
                return CottonAndroidBackgroundTransferScheduleResult.Unsupported(
                    request,
                    "Android did not accept the background transfer schedule request.");
            }

            Log.Info(
                LogTag,
                $"Scheduled Android UIDT job {request.ScheduleIdentity.JobId} for transfer {request.TransferId}.");
            return CottonAndroidBackgroundTransferScheduleResult.Scheduled(
                request,
                $"Scheduled Android background upload for {request.DisplayName}.");
        }

        private static JobInfo CreateJobInfo(
            Context context,
            CottonAndroidBackgroundTransferRequest request)
        {
            Java.Lang.Class serviceClass = Java.Lang.Class.FromType(
                    typeof(AndroidUserInitiatedTransferJobService))
                ?? throw new InvalidOperationException("Android transfer JobService type is unavailable.");
            var componentName = new ComponentName(
                context,
                serviceClass);
#pragma warning disable CA1416
            var builder = new JobInfo.Builder(request.ScheduleIdentity.JobId, componentName);
            builder.SetExtras(AndroidBackgroundTransferJobExtras.Create(request));
            builder.SetRequiredNetworkType(
                request.RequiresUnmeteredNetwork
                    ? NetworkType.Unmetered
                    : NetworkType.Any);
            builder.SetUserInitiated(true);

            if (request.EstimatedUploadBytes is long estimatedUploadBytes)
            {
                builder.SetEstimatedNetworkBytes(0L, estimatedUploadBytes);
            }

            if (request.RequiresCharging)
            {
                builder.SetRequiresCharging(true);
            }

            return builder.Build()
                ?? throw new InvalidOperationException("Android JobInfo builder returned no job.");
#pragma warning restore CA1416
        }
    }
}
#endif
