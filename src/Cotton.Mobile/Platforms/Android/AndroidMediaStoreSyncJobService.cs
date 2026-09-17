// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.App.Job;
using Android.Util;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    [Service(
        Name = ComponentName,
        Permission = AndroidMediaStoreSyncJobConstants.ServicePermission,
        Exported = false)]
    public class AndroidMediaStoreSyncJobService : JobService
    {
        public const string ComponentName = "dev.cottoncloud.mobile.AndroidMediaStoreSyncJobService";

        private const string LogTag = "CottonMediaSyncJob";
        private readonly Lock _executionGate = new();
        private AndroidJobExecution? _executionCancellation;

        public override bool OnStartJob(JobParameters? @params)
        {
            if (@params is null)
            {
                return false;
            }

            AndroidJobExecution cancellation = new();
            lock (_executionGate)
            {
                if (_executionCancellation is not null)
                {
                    _ = StopAsync(_executionCancellation);
                }

                _executionCancellation = cancellation;
            }

#if DEBUG
            _ = Log.Info(LogTag, "started");
#endif
            _ = Task.Run(() => ExecuteAsync(@params, cancellation));
            return true;
        }

        public override bool OnStopJob(JobParameters? @params)
        {
            ILogger<AndroidMediaStoreSyncJobService>? logger = IPlatformApplication.Current?.Services
                .GetService<ILogger<AndroidMediaStoreSyncJobService>>();
            if (logger is not null && @params is not null && OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                AndroidAutomaticSyncDiagnosticLog.MediaStoreStopped(logger, (int)@params.StopReason);
            }

            AndroidJobExecution? cancellation;
            lock (_executionGate)
            {
                cancellation = _executionCancellation;
                _executionCancellation = null;
            }

            if (cancellation is not null)
            {
                _ = StopAsync(cancellation);
            }

            return true;
        }

        private static async Task StopAsync(AndroidJobExecution cancellation)
        {
            try
            {
                await cancellation.CancelAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _ = Log.Error(LogTag, exception.ToString());
            }
        }

        private async Task ExecuteAsync(
            JobParameters parameters,
            AndroidJobExecution cancellation)
        {
            try
            {
                IServiceProvider services = IPlatformApplication.Current?.Services
                    ?? throw new InvalidOperationException("Android application services are unavailable.");
                AndroidAutomaticSyncExecutor executor = services
                    .GetRequiredService<AndroidAutomaticSyncExecutor>();
                AndroidAutomaticSyncExecutionResult result = await executor
                    .ExecuteAsync(
                        CottonAutomaticSyncTrigger.MediaStoreChanged,
                        retryRootId: null,
                        cancellation.Token)
                    .ConfigureAwait(false);
                switch (result)
                {
                    case AndroidAutomaticSyncExecutionResult.Completed:
                    case AndroidAutomaticSyncExecutionResult.NoSession:
                        ICottonAutomaticSyncBackgroundScheduler scheduler = services
                            .GetRequiredService<ICottonAutomaticSyncBackgroundScheduler>();
                        await scheduler
                            .RescheduleMediaStoreTriggerAsync(CancellationToken.None)
                            .ConfigureAwait(false);
#if DEBUG
                        _ = Log.Info(LogTag, "rescheduled");
#endif
                        CompleteIfRunning(parameters, cancellation, wantsReschedule: false);
                        break;

                    case AndroidAutomaticSyncExecutionResult.RetryRequired:
                        CompleteIfRunning(parameters, cancellation, wantsReschedule: true);
                        break;

                    default:
                        throw new InvalidOperationException("Sync execution result is not supported.");
                }
#if DEBUG
                _ = Log.Info(LogTag, "completed");
#endif
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                _ = Log.Debug(LogTag, "execution cancelled");
            }
            catch (Exception exception)
            {
                _ = Log.Error(LogTag, exception.ToString());
                CompleteIfRunning(parameters, cancellation, wantsReschedule: true);
            }
            finally
            {
                ClearExecution(cancellation);
                try
                {
                    await cancellation.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _ = Log.Error(LogTag, exception.ToString());
                }
            }
        }

        private void CompleteIfRunning(
            JobParameters parameters,
            AndroidJobExecution cancellation,
            bool wantsReschedule)
        {
            bool isRunning;
            lock (_executionGate)
            {
                isRunning = ReferenceEquals(_executionCancellation, cancellation);
                if (isRunning)
                {
                    _executionCancellation = null;
                }
            }

            if (isRunning)
            {
                JobFinished(parameters, wantsReschedule);
            }
        }

        private void ClearExecution(AndroidJobExecution cancellation)
        {
            lock (_executionGate)
            {
                if (ReferenceEquals(_executionCancellation, cancellation))
                {
                    _executionCancellation = null;
                }
            }
        }
    }
}
#endif
