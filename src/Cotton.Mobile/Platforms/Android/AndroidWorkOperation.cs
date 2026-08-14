// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using AndroidX.Core.Content;
using AndroidX.Work;
using Google.Common.Util.Concurrent;
using Java.Util.Concurrent;

namespace Cotton.Mobile.Platforms.Android
{
    internal static class AndroidWorkOperation
    {
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
