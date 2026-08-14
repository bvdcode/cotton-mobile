// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.Content;
using AndroidX.Work;
using Google.Common.Util.Concurrent;
using Java.Util.Concurrent;

namespace Cotton.Mobile.Platforms.Android
{
    internal static class AndroidWorkOperation
    {
        private const string RescheduleReceiverClassName =
            "androidx.work.impl.background.systemalarm.RescheduleReceiver";
        private const int RescheduleReceiverReadinessAttemptCount = 100;
        private static readonly TimeSpan RescheduleReceiverReadinessDelay =
            TimeSpan.FromMilliseconds(50);

        public static Task WaitAsync(
            IOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();

            IListenableFuture future = operation.Result
                ?? throw new InvalidOperationException("Android WorkManager operation result is unavailable.");
            TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            IExecutor executor = ContextCompat.GetMainExecutor(global::Android.App.Application.Context)
                ?? throw new InvalidOperationException("Android main executor is unavailable.");
            future.AddListener(
                new Java.Lang.Runnable(() => Complete(future, completion)),
                executor);
            return completion.Task.WaitAsync(cancellationToken);
        }

        public static async Task WaitForRescheduleReceiverAsync(
            CancellationToken cancellationToken = default)
        {
            Context context = global::Android.App.Application.Context;
            PackageManager packageManager = context.PackageManager
                ?? throw new InvalidOperationException("Android package manager is unavailable.");
            string packageName = context.PackageName
                ?? throw new InvalidOperationException("Android package name is unavailable.");
            using ComponentName componentName = new(
                packageName,
                RescheduleReceiverClassName);

            for (int attempt = 0; attempt < RescheduleReceiverReadinessAttemptCount; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ComponentEnabledState state = packageManager.GetComponentEnabledSetting(componentName);
                if (state == ComponentEnabledState.Enabled)
                {
                    return;
                }

                await Task.Delay(RescheduleReceiverReadinessDelay, cancellationToken).ConfigureAwait(false);
            }

            throw new InvalidOperationException(
                "Android WorkManager reschedule receiver did not become ready.");
        }

        private static void Complete(
            IListenableFuture future,
            TaskCompletionSource completion)
        {
            try
            {
                _ = future.Get();
                _ = completion.TrySetResult();
            }
            catch (System.Exception exception)
            {
                _ = completion.TrySetException(exception);
            }
        }
    }
}
#endif
