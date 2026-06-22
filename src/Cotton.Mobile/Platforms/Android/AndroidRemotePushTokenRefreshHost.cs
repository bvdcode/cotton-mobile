// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Util;
using AndroidX.Work;
using Java.Util.Concurrent;
using WorkNetworkType = AndroidX.Work.NetworkType;

namespace Cotton.Mobile.Services
{
    public class AndroidRemotePushTokenRefreshHost : ICottonAndroidRemotePushTokenRefreshHost
    {
        private const string LogTag = "CottonRemotePush";

        public Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
            CottonAndroidRemotePushTokenRefreshRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            Context context = Android.App.Application.Context;
            WorkManager workManager = WorkManager.GetInstance(context)
                ?? throw new InvalidOperationException("Android WorkManager is unavailable.");

            PeriodicWorkRequest workRequest = CreateWorkManagerRequest(request);
            ExistingPeriodicWorkPolicy keepExisting = ExistingPeriodicWorkPolicy.Keep
                ?? throw new InvalidOperationException("Android WorkManager KEEP periodic policy is unavailable.");
            _ = workManager.EnqueueUniquePeriodicWork(
                request.ScheduleIdentity.UniqueWorkName,
                keepExisting,
                workRequest);

            Log.Info(
                LogTag,
                $"Scheduled Android WorkManager remote push token refresh every {request.RefreshInterval.TotalDays:0.#} day(s).");
            return Task.FromResult(
                CottonAndroidRemotePushTokenRefreshScheduleResult.Scheduled(
                    request,
                    "Scheduled Android remote push token refresh."));
        }

        public Task<CottonAndroidRemotePushTokenRefreshCancelResult> CancelAsync(
            CottonAndroidRemotePushTokenRefreshScheduleIdentity scheduleIdentity,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scheduleIdentity);
            cancellationToken.ThrowIfCancellationRequested();

            Context context = Android.App.Application.Context;
            WorkManager workManager = WorkManager.GetInstance(context)
                ?? throw new InvalidOperationException("Android WorkManager is unavailable.");
            _ = workManager.CancelUniqueWork(scheduleIdentity.UniqueWorkName);

            Log.Info(
                LogTag,
                $"Cancelled Android WorkManager remote push token refresh {scheduleIdentity.UniqueWorkName}.");
            return Task.FromResult(CottonAndroidRemotePushTokenRefreshCancelResult.Cancelled());
        }

        private static PeriodicWorkRequest CreateWorkManagerRequest(
            CottonAndroidRemotePushTokenRefreshRequest request)
        {
            Java.Lang.Class workerClass = Java.Lang.Class.FromType(
                    typeof(AndroidRemotePushTokenRefreshWorker))
                ?? throw new InvalidOperationException("Android remote push token refresh worker type is unavailable.");
            Constraints constraints = CreateWorkManagerConstraints();
            long repeatIntervalMinutes = Convert.ToInt64(request.RefreshInterval.TotalMinutes);
            TimeUnit minutes = TimeUnit.Minutes
                ?? throw new InvalidOperationException("Android WorkManager minute time unit is unavailable.");

            return new PeriodicWorkRequest.Builder(
                    workerClass,
                    repeatIntervalMinutes,
                    minutes)
                .SetConstraints(constraints)
                .AddTag(request.ScheduleIdentity.RefreshTag)
                .Build()
                ?? throw new InvalidOperationException("Android WorkManager remote push token refresh builder returned no request.");
        }

        private static Constraints CreateWorkManagerConstraints()
        {
            var builder = new Constraints.Builder();
            builder.SetRequiredNetworkType(
                WorkNetworkType.Connected
                    ?? throw new InvalidOperationException("Android WorkManager connected network type is unavailable."));

            return builder.Build()
                ?? throw new InvalidOperationException("Android WorkManager remote push token refresh constraints builder returned no constraints.");
        }
    }
}
#endif
