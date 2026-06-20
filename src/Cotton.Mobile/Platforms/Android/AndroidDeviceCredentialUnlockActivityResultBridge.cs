#if ANDROID
using Android.App;
using Android.Content;

namespace Cotton.Mobile.Services
{
    public class AndroidDeviceCredentialUnlockActivityResultBridge : IAndroidDeviceCredentialUnlockActivityResultBridge
    {
        private const int RequestCode = 62013;

        private readonly object _syncRoot = new();
        private PendingDeviceUnlock? _pendingUnlock;

        public Task<CottonDeviceUnlockResult> RequestUnlockAsync(
            Activity activity,
            Intent intent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(activity);
            ArgumentNullException.ThrowIfNull(intent);
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<CottonDeviceUnlockResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;
            lock (_syncRoot)
            {
                if (_pendingUnlock is not null)
                {
                    throw new InvalidOperationException("Device unlock is already in progress.");
                }

                cancellationRegistration = cancellationToken.Register(() => CancelPending(completion));
                _pendingUnlock = new PendingDeviceUnlock(completion, cancellationRegistration);
            }

            try
            {
#pragma warning disable CS0618
                activity.StartActivityForResult(intent, RequestCode);
#pragma warning restore CS0618
            }
            catch (Exception)
            {
                PendingDeviceUnlock? pendingUnlock = ClearPending(completion);
                pendingUnlock?.Dispose();
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

            PendingDeviceUnlock? pendingUnlock = ClearPending();
            if (pendingUnlock is null)
            {
                return true;
            }

            pendingUnlock.Dispose();
            CottonDeviceUnlockResult result = resultCode == Result.Ok
                ? CottonDeviceUnlockResult.Succeeded
                : CottonDeviceUnlockResult.Cancelled;
            pendingUnlock.Completion.TrySetResult(result);
            return true;
        }

        private void CancelPending(TaskCompletionSource<CottonDeviceUnlockResult> completion)
        {
            PendingDeviceUnlock? pendingUnlock = ClearPending(completion);
            if (pendingUnlock is null)
            {
                return;
            }

            pendingUnlock.Dispose();
            pendingUnlock.Completion.TrySetCanceled();
        }

        private PendingDeviceUnlock? ClearPending(
            TaskCompletionSource<CottonDeviceUnlockResult>? expectedCompletion = null)
        {
            lock (_syncRoot)
            {
                if (_pendingUnlock is null)
                {
                    return null;
                }

                if (expectedCompletion is not null && !ReferenceEquals(_pendingUnlock.Completion, expectedCompletion))
                {
                    return null;
                }

                PendingDeviceUnlock pendingUnlock = _pendingUnlock;
                _pendingUnlock = null;
                return pendingUnlock;
            }
        }

        private class PendingDeviceUnlock
        {
            private readonly CancellationTokenRegistration _cancellationRegistration;

            public PendingDeviceUnlock(
                TaskCompletionSource<CottonDeviceUnlockResult> completion,
                CancellationTokenRegistration cancellationRegistration)
            {
                Completion = completion;
                _cancellationRegistration = cancellationRegistration;
            }

            public TaskCompletionSource<CottonDeviceUnlockResult> Completion { get; }

            public void Dispose()
            {
                _cancellationRegistration.Dispose();
            }
        }
    }
}
#endif
