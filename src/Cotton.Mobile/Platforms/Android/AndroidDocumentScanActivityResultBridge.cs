// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentScanActivityResultBridge : IAndroidDocumentScanActivityResultBridge
    {
        private const int RequestCode = 61027;

        private readonly object _syncRoot = new();
        private PendingDocumentScan? _pendingScan;

        public Task<Intent?> StartScanAsync(
            Activity activity,
            IntentSender intentSender,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(activity);
            ArgumentNullException.ThrowIfNull(intentSender);
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<Intent?>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;
            lock (_syncRoot)
            {
                if (_pendingScan is not null)
                {
                    throw new InvalidOperationException("A document scan is already in progress.");
                }

                cancellationRegistration = cancellationToken.Register(() => CancelPending(completion));
                _pendingScan = new PendingDocumentScan(completion, cancellationRegistration);
            }

            try
            {
                activity.StartIntentSenderForResult(intentSender, RequestCode, null, 0, 0, 0);
            }
            catch (Exception)
            {
                PendingDocumentScan? pendingScan = ClearPending(completion);
                pendingScan?.Dispose();
                throw;
            }

            return completion.Task;
        }

        public bool TryHandleActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode != RequestCode)
            {
                return false;
            }

            PendingDocumentScan? pendingScan = ClearPending();
            if (pendingScan is null)
            {
                return true;
            }

            pendingScan.Dispose();
            if (resultCode == Result.Ok && data is not null)
            {
                pendingScan.Completion.TrySetResult(data);
            }
            else
            {
                pendingScan.Completion.TrySetResult(null);
            }

            return true;
        }

        private void CancelPending(TaskCompletionSource<Intent?> completion)
        {
            PendingDocumentScan? pendingScan = ClearPending(completion);
            if (pendingScan is null)
            {
                return;
            }

            pendingScan.Dispose();
            pendingScan.Completion.TrySetCanceled();
        }

        private PendingDocumentScan? ClearPending(TaskCompletionSource<Intent?>? expectedCompletion = null)
        {
            lock (_syncRoot)
            {
                if (_pendingScan is null)
                {
                    return null;
                }

                if (expectedCompletion is not null && !ReferenceEquals(_pendingScan.Completion, expectedCompletion))
                {
                    return null;
                }

                PendingDocumentScan pendingScan = _pendingScan;
                _pendingScan = null;
                return pendingScan;
            }
        }

        private class PendingDocumentScan
        {
            private readonly CancellationTokenRegistration _cancellationRegistration;

            public PendingDocumentScan(
                TaskCompletionSource<Intent?> completion,
                CancellationTokenRegistration cancellationRegistration)
            {
                Completion = completion;
                _cancellationRegistration = cancellationRegistration;
            }

            public TaskCompletionSource<Intent?> Completion { get; }

            public void Dispose()
            {
                _cancellationRegistration.Dispose();
            }
        }
    }
}
#endif
