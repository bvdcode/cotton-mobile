// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using System.Collections.Concurrent;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using Android.Util;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace Cotton.Mobile.Services
{
    [Service(
        Name = "dev.cottoncloud.app.AndroidUserInitiatedTransferJobService",
        Permission = "android.permission.BIND_JOB_SERVICE",
        Exported = false)]
    public sealed class AndroidUserInitiatedTransferJobService : JobService
    {
        private const string LogTag = "CottonTransfer";

        private readonly ConcurrentDictionary<int, RunningTransferJob> _runningJobs = new();

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
            RunningTransferJob runningJob = CreateRunningTransferJob(
                parameters,
                transferId,
                displayName,
                workKind,
                cancellationTokenSource);
            _runningJobs[parameters.JobId] = runningJob;
            SetRunningNotification(parameters, transferId, displayName, workKind);
            _ = ExecuteTransferAsync(parameters, instanceUri, transferId, runningJob);
            return true;
        }

        public override bool OnStopJob(JobParameters? parameters)
        {
            if (parameters is null)
            {
                return false;
            }

            if (_runningJobs.TryRemove(parameters.JobId, out RunningTransferJob? runningJob))
            {
                runningJob.Cancel();
                runningJob.DetachProgress();
            }

            return true;
        }

        private RunningTransferJob CreateRunningTransferJob(
            JobParameters parameters,
            Guid transferId,
            string displayName,
            CottonAndroidTransferWorkKind workKind,
            CancellationTokenSource cancellationTokenSource)
        {
            ICottonTransferProgressSignal? progressSignal = IPlatformApplication.Current?.Services
                .GetService<ICottonTransferProgressSignal>();
            EventHandler<CottonTransferProgressChangedEventArgs>? progressHandler = null;
            if (progressSignal is not null)
            {
                progressHandler = (_, args) =>
                {
                    if (args.TransferId != transferId)
                    {
                        return;
                    }

                    SetRunningNotification(parameters, transferId, displayName, workKind, args.Progress);
                };
                progressSignal.TransferProgressChanged += progressHandler;
            }

            return new RunningTransferJob(cancellationTokenSource, progressSignal, progressHandler);
        }

        private async Task ExecuteTransferAsync(
            JobParameters parameters,
            Uri instanceUri,
            Guid transferId,
            RunningTransferJob runningJob)
        {
            bool needsReschedule = false;
            try
            {
                ICottonAndroidBackgroundTransferJobRunner? runner = IPlatformApplication.Current?.Services
                    .GetService<ICottonAndroidBackgroundTransferJobRunner>();
                if (runner is null)
                {
                    Log.Warn(LogTag, "Android UIDT job could not resolve Cotton background transfer runner.");
                    needsReschedule = true;
                    return;
                }

                CottonQueuedUploadExecutionResult result = await runner
                    .RunAsync(instanceUri, transferId, runningJob.CancellationToken)
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
                runningJob.Dispose();
                JobFinished(parameters, needsReschedule);
            }
        }

        private void SetRunningNotification(
            JobParameters parameters,
            Guid transferId,
            string displayName,
            CottonAndroidTransferWorkKind workKind,
            CottonTransferProgressSnapshot? progress = null)
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
                    workKind,
                    progress);
#pragma warning disable CA1416
                UpdateNetworkBytes(parameters, progress);
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
            CottonAndroidTransferWorkKind workKind,
            CottonTransferProgressSnapshot? progress)
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
                .SetContentText(CreateNotificationText(displayName, progress))
                .SetSmallIcon(Resource.Drawable.ic_stat_cotton_cloud)
                .SetColor(GetColor(Resource.Color.cotton_accent))
                .SetOngoing(true)
                .SetOnlyAlertOnce(true)
                .SetShowWhen(true)
                .SetCategory(Notification.CategoryProgress)
                .SetSubText(transferId.ToString("N")[..8]);

            int? percent = progress?.Percent;
            if (percent.HasValue)
            {
                builder.SetProgress(100, percent.Value, false);
            }
            else
            {
                builder.SetProgress(0, 0, true);
            }

            if (pendingIntent is not null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            return builder.Build();
        }

        private void UpdateNetworkBytes(
            JobParameters parameters,
            CottonTransferProgressSnapshot? progress)
        {
            if (progress is null || (int)Build.VERSION.SdkInt < 34)
            {
                return;
            }

#pragma warning disable CA1416
            if (progress.TotalBytes is long totalBytes)
            {
                UpdateEstimatedNetworkBytes(parameters, 0, totalBytes);
            }

            UpdateTransferredNetworkBytes(parameters, 0, progress.TransferredBytes);
#pragma warning restore CA1416
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

        private static string CreateNotificationText(
            string displayName,
            CottonTransferProgressSnapshot? progress)
        {
            if (progress is null)
            {
                return $"{displayName} is uploading.";
            }

            if (progress.TotalBytes is > 0)
            {
                return $"{displayName}: {progress.DisplayText} uploaded.";
            }

            if (progress.TransferredBytes > 0)
            {
                return $"{displayName}: {CottonFileSizeFormatter.Format(progress.TransferredBytes)} uploaded.";
            }

            return $"{displayName} is uploading.";
        }

        private class RunningTransferJob : IDisposable
        {
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly ICottonTransferProgressSignal? _progressSignal;
            private readonly EventHandler<CottonTransferProgressChangedEventArgs>? _progressHandler;
            private int _isProgressDetached;

            public RunningTransferJob(
                CancellationTokenSource cancellationTokenSource,
                ICottonTransferProgressSignal? progressSignal,
                EventHandler<CottonTransferProgressChangedEventArgs>? progressHandler)
            {
                ArgumentNullException.ThrowIfNull(cancellationTokenSource);

                _cancellationTokenSource = cancellationTokenSource;
                _progressSignal = progressSignal;
                _progressHandler = progressHandler;
            }

            public CancellationToken CancellationToken => _cancellationTokenSource.Token;

            public void Cancel()
            {
                _cancellationTokenSource.Cancel();
            }

            public void DetachProgress()
            {
                if (Interlocked.Exchange(ref _isProgressDetached, 1) == 1)
                {
                    return;
                }

                if (_progressSignal is not null && _progressHandler is not null)
                {
                    _progressSignal.TransferProgressChanged -= _progressHandler;
                }
            }

            public void Dispose()
            {
                DetachProgress();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
#endif
