// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidDocumentTreeActivityResultBridge(
        AndroidDocumentTreeActivityResultStore resultStore,
        ILogger<AndroidDocumentTreeActivityResultBridge> logger) :
        IAndroidDocumentTreeActivityResultBridge,
        IDisposable
    {
        private readonly Lock _syncRoot = new();
        private readonly ILogger<AndroidDocumentTreeActivityResultBridge> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly AndroidDocumentTreeActivityResultStore _resultStore =
            resultStore ?? throw new ArgumentNullException(nameof(resultStore));
        private PendingDocumentTreePick? _pendingPick;

        public Task<Intent?> StartOpenDocumentTreeAsync(
            Activity activity,
            Intent intent,
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(activity);
            ArgumentNullException.ThrowIfNull(intent);
            if (requestId == Guid.Empty)
            {
                throw new ArgumentException("Document-tree request id is required.", nameof(requestId));
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (_resultStore.TryReadResult(requestId, out Intent? savedResult))
            {
                return Task.FromResult(savedResult);
            }

            TaskCompletionSource<Intent?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;
            bool shouldLaunch;
            lock (_syncRoot)
            {
                if (_pendingPick is not null)
                {
                    throw new InvalidOperationException("A folder picker is already in progress.");
                }

                cancellationRegistration = cancellationToken.Register(() => CancelPending(completion));
                _pendingPick = new PendingDocumentTreePick(
                    requestId,
                    completion,
                    cancellationRegistration);
                shouldLaunch = !_resultStore.IsActiveInCurrentProcess(requestId);
                if (shouldLaunch)
                {
                    _resultStore.Begin(requestId);
                }
            }

            try
            {
                if (!shouldLaunch)
                {
                    return completion.Task;
                }

                MainActivity mainActivity = activity as MainActivity
                    ?? throw new InvalidOperationException("Document-tree picker requires the main Android activity.");
                mainActivity.LaunchDocumentTree(intent);
            }
            catch (Exception exception)
            {
                PendingDocumentTreePick? pendingPick = ClearPending(completion);
                pendingPick?.Dispose();
                _resultStore.Complete(requestId);
                CottonLog.Error(_logger, "Failed to start the Android document-tree picker.", exception);
                throw;
            }

            return completion.Task;
        }

        public void HandleActivityResult(Result resultCode, Intent? data)
        {
            Guid? requestId = _resultStore.SaveResult(resultCode, data);
            if (!requestId.HasValue)
            {
                return;
            }

            PendingDocumentTreePick? pendingPick = ClearPending();
            if (pendingPick is null || pendingPick.RequestId != requestId.Value)
            {
                return;
            }

            pendingPick.Dispose();
            if (!_resultStore.TryReadResult(requestId.Value, out Intent? savedResult))
            {
                pendingPick.Completion.TrySetException(
                    new InvalidDataException("Android document-tree result was not saved."));
                return;
            }

            pendingPick.Completion.TrySetResult(savedResult);
        }

        public void CompleteRequest(Guid requestId)
        {
            _resultStore.Complete(requestId);
        }

        public void Dispose()
        {
            PendingDocumentTreePick? pendingPick = ClearPending();
            pendingPick?.Dispose();
            pendingPick?.Completion.TrySetCanceled();
            GC.SuppressFinalize(this);
        }

        private void CancelPending(TaskCompletionSource<Intent?> completion)
        {
            PendingDocumentTreePick? pendingPick = ClearPending(completion);
            if (pendingPick is null)
            {
                return;
            }

            pendingPick.Dispose();
            _resultStore.Complete(pendingPick.RequestId);
            pendingPick.Completion.TrySetCanceled();
        }

        private PendingDocumentTreePick? ClearPending(TaskCompletionSource<Intent?>? expectedCompletion = null)
        {
            lock (_syncRoot)
            {
                if (_pendingPick is null)
                {
                    return null;
                }

                if (expectedCompletion is not null && !ReferenceEquals(_pendingPick.Completion, expectedCompletion))
                {
                    return null;
                }

                PendingDocumentTreePick pendingPick = _pendingPick;
                _pendingPick = null;
                return pendingPick;
            }
        }
    }
}
#endif
