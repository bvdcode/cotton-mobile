#if ANDROID
using System.Collections.Concurrent;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using Android.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Services
{
    [Service(
        Name = "dev.cottoncloud.app.AndroidUserInitiatedTransferJobService",
        Permission = "android.permission.BIND_JOB_SERVICE",
        Exported = false)]
    public sealed class AndroidUserInitiatedTransferJobService : JobService
    {
        private const string LogTag = "CottonTransfer";

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _runningJobs = new();

        public override bool OnStartJob(JobParameters? parameters)
        {
            if (parameters is null)
            {
                return false;
            }

            if (!AndroidBackgroundTransferJobExtras.TryRead(
                    parameters.Extras,
                    out Uri? instanceUri,
                    out Guid transferId,
                    out string displayName,
                    out CottonAndroidTransferWorkKind workKind)
                || instanceUri is null)
            {
                Log.Warn(LogTag, $"Android UIDT job {parameters.JobId} started without valid Cotton transfer extras.");
                JobFinished(parameters, false);
                return false;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            _runningJobs[parameters.JobId] = cancellationTokenSource;
            SetRunningNotification(parameters, transferId, displayName, workKind);
            _ = ExecuteTransferAsync(parameters, instanceUri, transferId, cancellationTokenSource);
            return true;
        }

        public override bool OnStopJob(JobParameters? parameters)
        {
            if (parameters is null)
            {
                return false;
            }

            if (_runningJobs.TryRemove(parameters.JobId, out CancellationTokenSource? cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }

            return true;
        }

        private async Task ExecuteTransferAsync(
            JobParameters parameters,
            Uri instanceUri,
            Guid transferId,
            CancellationTokenSource cancellationTokenSource)
        {
            bool needsReschedule = false;
            try
            {
                ICottonQueuedUploadExecutor? executor = IPlatformApplication.Current?.Services
                    .GetService<ICottonQueuedUploadExecutor>();
                if (executor is null)
                {
                    Log.Warn(LogTag, "Android UIDT job could not resolve Cotton queued upload executor.");
                    needsReschedule = true;
                    return;
                }

                CottonQueuedUploadExecutionResult result = await executor
                    .ExecuteAsync(instanceUri, transferId, cancellationTokenSource.Token)
                    .ConfigureAwait(false);
                Log.Info(
                    LogTag,
                    $"Android UIDT job {parameters.JobId} finished transfer {transferId} with status {result.Status}.");
            }
            catch (System.OperationCanceledException)
            {
                needsReschedule = true;
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Android UIDT job {parameters.JobId} failed before transfer completion. {exception}");
                needsReschedule = true;
            }
            finally
            {
                _runningJobs.TryRemove(parameters.JobId, out _);
                cancellationTokenSource.Dispose();
                JobFinished(parameters, needsReschedule);
            }
        }

        private void SetRunningNotification(
            JobParameters parameters,
            Guid transferId,
            string displayName,
            CottonAndroidTransferWorkKind workKind)
        {
            if ((int)Build.VERSION.SdkInt < 34)
            {
                return;
            }

            try
            {
                IPlatformApplication.Current?.Services
                    .GetService<ICottonNotificationChannelProvisioningService>()
                    ?.EnsureChannels();

                Notification notification = BuildRunningNotification(
                    parameters.JobId,
                    transferId,
                    displayName,
                    workKind);
#pragma warning disable CA1416
                SetNotification(
                    parameters,
                    parameters.JobId,
                    notification,
                    JobEndNotificationPolicy.Remove);
#pragma warning restore CA1416
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Android UIDT job {parameters.JobId} could not attach notification. {exception}");
            }
        }

        private Notification BuildRunningNotification(
            int jobId,
            Guid transferId,
            string displayName,
            CottonAndroidTransferWorkKind workKind)
        {
            CottonNotificationChannelSnapshot channel =
                CottonNotificationChannelCatalog.Get(CottonNotificationChannelKind.Transfers);
            Notification.Builder builder;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
#pragma warning disable CA1416
                builder = new Notification.Builder(this, channel.Id);
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CA1422
                builder = new Notification.Builder(this);
#pragma warning restore CA1422
            }

            PendingIntent? pendingIntent = CreateLaunchPendingIntent(jobId);
            builder
                .SetContentTitle(CreateNotificationTitle(workKind))
                .SetContentText($"{displayName} is uploading.")
                .SetSmallIcon(ApplicationInfo?.Icon ?? Resource.Mipmap.appicon)
                .SetOngoing(true)
                .SetOnlyAlertOnce(true)
                .SetShowWhen(true)
                .SetProgress(0, 0, true)
                .SetCategory(Notification.CategoryProgress)
                .SetSubText(transferId.ToString("N")[..8]);

            if (pendingIntent is not null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            return builder.Build();
        }

        private PendingIntent? CreateLaunchPendingIntent(int jobId)
        {
            Intent? launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
            if (launchIntent is null)
            {
                return null;
            }

            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
#pragma warning disable CA1416
                flags |= PendingIntentFlags.Immutable;
#pragma warning restore CA1416
            }

            return PendingIntent.GetActivity(this, jobId, launchIntent, flags);
        }

        private static string CreateNotificationTitle(CottonAndroidTransferWorkKind workKind)
        {
            return workKind switch
            {
                CottonAndroidTransferWorkKind.ShareInboxUpload => "Capture upload running",
                CottonAndroidTransferWorkKind.ManualUpload => "Upload running",
                _ => "Transfer running",
            };
        }
    }
}
#endif
