// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.App.Job;
using Android.Util;
using Cotton.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;

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
        private CancellationTokenSource? _executionCancellation;

        public override bool OnStartJob(JobParameters? @params)
        {
            if (@params is null)
            {
                return false;
            }

            CancellationTokenSource cancellation = new();
            lock (_executionGate)
            {
                _executionCancellation?.Cancel();
                _executionCancellation = cancellation;
            }

#if DEBUG
            _ = Log.Info(LogTag, "started");
#endif
            _ = ExecuteAsync(@params, cancellation);
            return true;
        }

        public override bool OnStopJob(JobParameters? @params)
        {
            CancellationTokenSource? cancellation;
            lock (_executionGate)
            {
                cancellation = _executionCancellation;
                _executionCancellation = null;
            }

            cancellation?.Cancel();
            return true;
        }

        private async Task ExecuteAsync(
            JobParameters parameters,
            CancellationTokenSource cancellation)
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
                        ICottonAutomaticSyncBackgroundScheduler scheduler = services
                            .GetRequiredService<ICottonAutomaticSyncBackgroundScheduler>();
                        await scheduler
                            .RescheduleMediaStoreTriggerAsync(CancellationToken.None)
                            .ConfigureAwait(false);
#if DEBUG
                        _ = Log.Info(LogTag, "rescheduled");
#endif
                        break;

                    case AndroidAutomaticSyncExecutionResult.NoSession:
                        CompleteIfRunning(parameters, cancellation, wantsReschedule: false);
                        break;

                    case AndroidAutomaticSyncExecutionResult.RetryRequired:
                        throw new InvalidOperationException(
                            "MediaStore sync unexpectedly requested a direct retry.");

                    default:
                        throw new InvalidOperationException("Sync execution result is not supported.");
                }
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
                cancellation.Dispose();
            }
        }

        private void CompleteIfRunning(
            JobParameters parameters,
            CancellationTokenSource cancellation,
            bool wantsReschedule)
        {
            bool isRunning;
            lock (_executionGate)
            {
                isRunning = ReferenceEquals(_executionCancellation, cancellation);
            }

            if (isRunning)
            {
                JobFinished(parameters, wantsReschedule);
            }
        }

        private void ClearExecution(CancellationTokenSource cancellation)
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
