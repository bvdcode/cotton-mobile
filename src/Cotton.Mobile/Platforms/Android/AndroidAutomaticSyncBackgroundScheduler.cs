// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Java.Util.Concurrent;
using WorkNetworkType = AndroidX.Work.NetworkType;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidAutomaticSyncBackgroundScheduler : ICottonAutomaticSyncBackgroundScheduler
    {
        public async Task ScheduleAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WorkManager workManager = GetWorkManager();
            ExistingPeriodicWorkPolicy periodicPolicy = ExistingPeriodicWorkPolicy.Keep
                ?? throw new InvalidOperationException("Android periodic KEEP policy is unavailable.");
            IOperation periodicOperation = workManager.EnqueueUniquePeriodicWork(
                AndroidAutomaticSyncConstants.PeriodicWorkName,
                periodicPolicy,
                CreatePeriodicRequest())
                ?? throw new InvalidOperationException("Android periodic sync operation is unavailable.");
            IOperation legacyMediaStoreCancellation = workManager.CancelUniqueWork(
                AndroidAutomaticSyncConstants.LegacyMediaStoreWorkName)
                ?? throw new InvalidOperationException("Legacy Android MediaStore sync cancellation is unavailable.");
            Task periodicTask = AndroidWorkOperation.WaitAsync(periodicOperation, cancellationToken);
            Task legacyMediaStoreTask = AndroidWorkOperation.WaitAsync(
                legacyMediaStoreCancellation,
                cancellationToken);
            await Task.WhenAll(periodicTask, legacyMediaStoreTask).ConfigureAwait(false);
            AndroidMediaStoreSyncJobScheduler.Schedule();
        }

        public Task RescheduleMediaStoreTriggerAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AndroidMediaStoreSyncJobScheduler.Schedule();
            return Task.CompletedTask;
        }

        public async Task ScheduleRootRetriesAsync(
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rootIds);
            cancellationToken.ThrowIfCancellationRequested();

            WorkManager workManager = GetWorkManager();
            ExistingWorkPolicy policy = GetKeepPolicy();
            List<Task> operationTasks = [];
            foreach (Guid rootId in rootIds.Distinct().Order())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (rootId == Guid.Empty)
                {
                    throw new ArgumentException("Sync root ids cannot be empty.", nameof(rootIds));
                }

                IOperation operation = workManager.EnqueueUniqueWork(
                    AndroidAutomaticSyncConstants.CreateRootRetryWorkName(rootId),
                    policy,
                    CreateRootRetryRequest(rootId))
                    ?? throw new InvalidOperationException("Android sync-root retry operation is unavailable.");
                operationTasks.Add(AndroidWorkOperation.WaitAsync(operation, cancellationToken));
            }

            if (operationTasks.Count == 0)
            {
                return;
            }

            await Task.WhenAll(operationTasks).ConfigureAwait(false);
        }

        public async Task CancelAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WorkManager workManager = GetWorkManager();
            IOperation periodicOperation = workManager.CancelUniqueWork(AndroidAutomaticSyncConstants.PeriodicWorkName)
                ?? throw new InvalidOperationException("Android periodic sync cancellation is unavailable.");
            IOperation legacyMediaStoreOperation = workManager.CancelUniqueWork(
                AndroidAutomaticSyncConstants.LegacyMediaStoreWorkName)
                ?? throw new InvalidOperationException("Legacy Android MediaStore sync cancellation is unavailable.");
            IOperation retryOperation = workManager.CancelAllWorkByTag(AndroidAutomaticSyncConstants.RootRetryTag)
                ?? throw new InvalidOperationException("Android sync-root retry cancellation is unavailable.");
            Task periodicTask = AndroidWorkOperation.WaitAsync(periodicOperation, cancellationToken);
            Task legacyMediaStoreTask = AndroidWorkOperation.WaitAsync(
                legacyMediaStoreOperation,
                cancellationToken);
            Task retryTask = AndroidWorkOperation.WaitAsync(retryOperation, cancellationToken);
            AndroidMediaStoreSyncJobScheduler.Cancel();
            await Task.WhenAll(periodicTask, legacyMediaStoreTask, retryTask).ConfigureAwait(false);
        }

        private static PeriodicWorkRequest CreatePeriodicRequest()
        {
            Java.Lang.Class workerClass = Java.Lang.Class.FromType(typeof(AndroidPeriodicSyncWorker))
                ?? throw new InvalidOperationException("Android periodic sync worker type is unavailable.");
            TimeUnit timeUnit = TimeUnit.Minutes
                ?? throw new InvalidOperationException("Android minute time unit is unavailable.");
            PeriodicWorkRequest request = new PeriodicWorkRequest.Builder(
                workerClass,
                AndroidAutomaticSyncConstants.PeriodicIntervalMinutes,
                timeUnit)
                .SetConstraints(CreateNetworkConstraints())
                .Build();
            return request
                ?? throw new InvalidOperationException("Android periodic sync work request is unavailable.");
        }

        private static OneTimeWorkRequest CreateRootRetryRequest(Guid rootId)
        {
            Java.Lang.Class workerClass = Java.Lang.Class.FromType(typeof(AndroidSyncRootWorker))
                ?? throw new InvalidOperationException("Android sync-root worker type is unavailable.");
            Data inputData = new Data.Builder()
                .PutString(AndroidAutomaticSyncConstants.RootIdInputKey, rootId.ToString("D"))
                .Build()
                ?? throw new InvalidOperationException("Android sync-root input data is unavailable.");
            OneTimeWorkRequest request = new OneTimeWorkRequest.Builder(workerClass)
                .SetInputData(inputData)
                .SetConstraints(CreateNetworkConstraints())
                .AddTag(AndroidAutomaticSyncConstants.RootRetryTag)
                .Build();
            return request
                ?? throw new InvalidOperationException("Android sync-root work request is unavailable.");
        }

        private static Constraints CreateNetworkConstraints()
        {
            WorkNetworkType networkType = WorkNetworkType.Connected
                ?? throw new InvalidOperationException("Android connected network type is unavailable.");
            Constraints constraints = new Constraints.Builder()
                .SetRequiredNetworkType(networkType)
                .Build();
            return constraints
                ?? throw new InvalidOperationException("Android sync work constraints are unavailable.");
        }

        private static ExistingWorkPolicy GetKeepPolicy()
        {
            return ExistingWorkPolicy.Keep
                ?? throw new InvalidOperationException("Android KEEP policy is unavailable.");
        }

        private static WorkManager GetWorkManager()
        {
            Context context = global::Android.App.Application.Context;
            return WorkManager.GetInstance(context)
                ?? throw new InvalidOperationException("Android WorkManager is unavailable.");
        }
    }
}
#endif
