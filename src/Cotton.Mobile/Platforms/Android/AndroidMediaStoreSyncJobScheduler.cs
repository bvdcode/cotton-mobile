// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App.Job;
using Android.Content;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidMediaStoreSyncJobScheduler
    {
        public static void Schedule()
        {
            Context context = global::Android.App.Application.Context;
            JobScheduler scheduler = GetScheduler(context);
            using Java.Lang.Class serviceClass = Java.Lang.Class.FromType(
                typeof(AndroidMediaStoreSyncJobService))
                ?? throw new InvalidOperationException("Android MediaStore sync job type is unavailable.");
            using ComponentName service = new(context, serviceClass);
            using JobInfo.Builder builder = new(AndroidMediaStoreSyncJobConstants.JobId, service);

            _ = builder.SetRequiredNetworkType(NetworkType.Any);
            _ = builder.SetTriggerContentUpdateDelay(
                AndroidMediaStoreSyncJobConstants.TriggerUpdateDelayMilliseconds);
            _ = builder.SetTriggerContentMaxDelay(
                AndroidMediaStoreSyncJobConstants.TriggerMaximumDelayMilliseconds);
            AddTrigger(builder, GetImagesUri());
            AddTrigger(builder, GetVideosUri());

            using JobInfo job = builder.Build()
                ?? throw new InvalidOperationException("Android MediaStore sync job is unavailable.");
            int result = scheduler.Schedule(job);
            if (result != JobScheduler.ResultSuccess)
            {
                throw new InvalidOperationException("Android MediaStore sync job was rejected.");
            }
        }

        public static void Cancel()
        {
            Context context = global::Android.App.Application.Context;
            GetScheduler(context).Cancel(AndroidMediaStoreSyncJobConstants.JobId);
        }

        private static void AddTrigger(JobInfo.Builder builder, AndroidUri uri)
        {
            using JobInfo.TriggerContentUri trigger = new(
                uri,
                TriggerContentUriFlags.NotifyForDescendants);
            _ = builder.AddTriggerContentUri(trigger);
        }

        private static JobScheduler GetScheduler(Context context)
        {
            return context.GetSystemService(Context.JobSchedulerService) as JobScheduler
                ?? throw new InvalidOperationException("Android JobScheduler is unavailable.");
        }

        private static AndroidUri GetImagesUri()
        {
            return MediaStore.Images.Media.ExternalContentUri
                ?? throw new InvalidOperationException("Android images URI is unavailable.");
        }

        private static AndroidUri GetVideosUri()
        {
            return MediaStore.Video.Media.ExternalContentUri
                ?? throw new InvalidOperationException("Android videos URI is unavailable.");
        }
    }
}
#endif
