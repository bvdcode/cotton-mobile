// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonNotificationLaunchState : ICottonNotificationLaunchState
    {
        private readonly Lock _gate = new();
        private readonly Queue<CottonNotificationLaunchRequest> _pendingLaunches = new();

        public event EventHandler? NotificationLaunchRequested;

        public int PendingNotificationLaunchCount
        {
            get
            {
                lock (_gate)
                {
                    return _pendingLaunches.Count;
                }
            }
        }

        public void NotifyNotificationOpened(CottonNotificationLaunchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            lock (_gate)
            {
                _pendingLaunches.Enqueue(request);
            }

            NotificationLaunchRequested?.Invoke(this, EventArgs.Empty);
        }

        public CottonNotificationLaunchRequest? TryConsumePendingNotificationLaunch()
        {
            lock (_gate)
            {
                return _pendingLaunches.Count == 0 ? null : _pendingLaunches.Dequeue();
            }
        }

        public void ClearPendingNotificationLaunches()
        {
            lock (_gate)
            {
                _pendingLaunches.Clear();
            }
        }
    }
}
