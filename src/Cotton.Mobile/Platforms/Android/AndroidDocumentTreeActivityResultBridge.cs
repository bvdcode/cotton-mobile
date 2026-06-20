#if ANDROID
using Android.App;
using Android.Content;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeActivityResultBridge : IAndroidDocumentTreeActivityResultBridge
    {
        private const int RequestCode = 61029;

        private readonly object _syncRoot = new();
        private PendingDocumentTreePick? _pendingPick;

        public Task<Intent?> StartOpenDocumentTreeAsync(
            Activity activity,
            Intent intent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(activity);
            ArgumentNullException.ThrowIfNull(intent);
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<Intent?>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;
            lock (_syncRoot)
            {
                if (_pendingPick is not null)
                {
                    throw new InvalidOperationException("A folder picker is already in progress.");
                }

                cancellationRegistration = cancellationToken.Register(() => CancelPending(completion));
                _pendingPick = new PendingDocumentTreePick(completion, cancellationRegistration);
            }

            try
            {
                activity.StartActivityForResult(intent, RequestCode);
            }
            catch (Exception)
            {
                PendingDocumentTreePick? pendingPick = ClearPending(completion);
                pendingPick?.Dispose();
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

            PendingDocumentTreePick? pendingPick = ClearPending();
            if (pendingPick is null)
            {
                return true;
            }

            pendingPick.Dispose();
            if (resultCode == Result.Ok && data is not null)
            {
                pendingPick.Completion.TrySetResult(data);
            }
            else
            {
                pendingPick.Completion.TrySetResult(null);
            }

            return true;
        }

        private void CancelPending(TaskCompletionSource<Intent?> completion)
        {
            PendingDocumentTreePick? pendingPick = ClearPending(completion);
            if (pendingPick is null)
            {
                return;
            }

            pendingPick.Dispose();
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

        private class PendingDocumentTreePick
        {
            private readonly CancellationTokenRegistration _cancellationRegistration;

            public PendingDocumentTreePick(
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
