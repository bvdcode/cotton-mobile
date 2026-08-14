// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Content;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Java.Util.Concurrent;
using WorkNetworkType = AndroidX.Work.NetworkType;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidNotificationBackgroundScheduler : ICottonNotificationBackgroundScheduler
    {
        public async Task ScheduleAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Context context = global::Android.App.Application.Context;
            WorkManager workManager = WorkManager.GetInstance(context)
                ?? throw new InvalidOperationException("Android WorkManager is unavailable.");
            PeriodicWorkRequest request = CreateRequest();
            ExistingPeriodicWorkPolicy policy = ExistingPeriodicWorkPolicy.Keep
                ?? throw new InvalidOperationException("Android periodic KEEP policy is unavailable.");
            IOperation operation = workManager.EnqueueUniquePeriodicWork(
                AndroidNotificationConstants.PeriodicWorkName,
                policy,
                request)
                ?? throw new InvalidOperationException("Android notification polling operation is unavailable.");
            await AndroidWorkOperation.WaitAsync(operation, cancellationToken).ConfigureAwait(false);
            await AndroidWorkOperation.WaitForRescheduleReceiverAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task CancelAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Context context = global::Android.App.Application.Context;
            WorkManager workManager = WorkManager.GetInstance(context)
                ?? throw new InvalidOperationException("Android WorkManager is unavailable.");
            IOperation operation = workManager.CancelUniqueWork(AndroidNotificationConstants.PeriodicWorkName)
                ?? throw new InvalidOperationException("Android notification polling cancellation is unavailable.");
            await AndroidWorkOperation.WaitAsync(operation, cancellationToken).ConfigureAwait(false);
        }

        private static PeriodicWorkRequest CreateRequest()
        {
            Java.Lang.Class workerClass = Java.Lang.Class.FromType(typeof(AndroidNotificationPollingWorker))
                ?? throw new InvalidOperationException("Android notification worker type is unavailable.");
            TimeUnit timeUnit = TimeUnit.Minutes
                ?? throw new InvalidOperationException("Android minute time unit is unavailable.");
            PeriodicWorkRequest request = new PeriodicWorkRequest.Builder(
                workerClass,
                AndroidNotificationConstants.PeriodicIntervalMinutes,
                timeUnit)
                .SetConstraints(CreateConstraints())
                .Build();
            return request
                ?? throw new InvalidOperationException("Android notification work request is unavailable.");
        }

        private static Constraints CreateConstraints()
        {
            WorkNetworkType networkType = WorkNetworkType.Connected
                ?? throw new InvalidOperationException("Android connected network type is unavailable.");
            Constraints constraints = new Constraints.Builder()
                .SetRequiredNetworkType(networkType)
                .Build();
            return constraints
                ?? throw new InvalidOperationException("Android notification work constraints are unavailable.");
        }
    }
}
