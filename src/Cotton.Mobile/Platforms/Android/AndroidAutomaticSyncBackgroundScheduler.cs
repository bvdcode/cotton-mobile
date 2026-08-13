// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Provider;
using AndroidX.Work;
using Cotton.Mobile.Services;
using Java.Util.Concurrent;
using WorkNetworkType = AndroidX.Work.NetworkType;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidAutomaticSyncBackgroundScheduler : ICottonAutomaticSyncBackgroundScheduler
    {
        public Task ScheduleAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WorkManager workManager = GetWorkManager();
            ExistingPeriodicWorkPolicy periodicPolicy = ExistingPeriodicWorkPolicy.Keep
                ?? throw new InvalidOperationException("Android periodic KEEP policy is unavailable.");
            _ = workManager.EnqueueUniquePeriodicWork(
                AndroidAutomaticSyncConstants.PeriodicWorkName,
                periodicPolicy,
                CreatePeriodicRequest());
            EnqueueMediaStoreTrigger(workManager, GetKeepPolicy());
            return Task.CompletedTask;
        }

        public Task RescheduleMediaStoreTriggerAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ExistingWorkPolicy policy = ExistingWorkPolicy.AppendOrReplace
                ?? throw new InvalidOperationException("Android APPEND_OR_REPLACE policy is unavailable.");
            EnqueueMediaStoreTrigger(GetWorkManager(), policy);
            return Task.CompletedTask;
        }

        public Task ScheduleRootRetriesAsync(
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rootIds);
            cancellationToken.ThrowIfCancellationRequested();

            WorkManager workManager = GetWorkManager();
            ExistingWorkPolicy policy = GetKeepPolicy();
            foreach (Guid rootId in rootIds.Distinct().Order())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (rootId == Guid.Empty)
                {
                    throw new ArgumentException("Sync root ids cannot be empty.", nameof(rootIds));
                }

                _ = workManager.EnqueueUniqueWork(
                    AndroidAutomaticSyncConstants.CreateRootRetryWorkName(rootId),
                    policy,
                    CreateRootRetryRequest(rootId));
            }

            return Task.CompletedTask;
        }

        public Task CancelAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WorkManager workManager = GetWorkManager();
            _ = workManager.CancelUniqueWork(AndroidAutomaticSyncConstants.PeriodicWorkName);
            _ = workManager.CancelUniqueWork(AndroidAutomaticSyncConstants.MediaStoreWorkName);
            _ = workManager.CancelAllWorkByTag(AndroidAutomaticSyncConstants.RootRetryTag);
            return Task.CompletedTask;
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

        private static OneTimeWorkRequest CreateMediaStoreRequest()
        {
            Java.Lang.Class workerClass = Java.Lang.Class.FromType(typeof(AndroidMediaStoreSyncWorker))
                ?? throw new InvalidOperationException("Android MediaStore sync worker type is unavailable.");
            OneTimeWorkRequest request = new OneTimeWorkRequest.Builder(workerClass)
                .SetConstraints(CreateMediaStoreConstraints())
                .Build();
            return request
                ?? throw new InvalidOperationException("Android MediaStore sync work request is unavailable.");
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

        private static Constraints CreateMediaStoreConstraints()
        {
            WorkNetworkType networkType = WorkNetworkType.Connected
                ?? throw new InvalidOperationException("Android connected network type is unavailable.");
            Constraints constraints = new Constraints.Builder()
                .SetRequiredNetworkType(networkType)
                .AddContentUriTrigger(GetImagesUri(), triggerForDescendants: true)
                .AddContentUriTrigger(GetVideosUri(), triggerForDescendants: true)
                .Build();
            return constraints
                ?? throw new InvalidOperationException("Android MediaStore sync work constraints are unavailable.");
        }

        private static void EnqueueMediaStoreTrigger(
            WorkManager workManager,
            ExistingWorkPolicy policy)
        {
            _ = workManager.EnqueueUniqueWork(
                AndroidAutomaticSyncConstants.MediaStoreWorkName,
                policy,
                CreateMediaStoreRequest());
        }

        private static ExistingWorkPolicy GetKeepPolicy()
        {
            return ExistingWorkPolicy.Keep
                ?? throw new InvalidOperationException("Android KEEP policy is unavailable.");
        }

        private static global::Android.Net.Uri GetImagesUri()
        {
            return MediaStore.Images.Media.ExternalContentUri
                ?? throw new InvalidOperationException("Android images URI is unavailable.");
        }

        private static global::Android.Net.Uri GetVideosUri()
        {
            return MediaStore.Video.Media.ExternalContentUri
                ?? throw new InvalidOperationException("Android videos URI is unavailable.");
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
