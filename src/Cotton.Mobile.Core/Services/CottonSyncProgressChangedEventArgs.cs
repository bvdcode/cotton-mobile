// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncProgressChangedEventArgs : EventArgs
    {
        public CottonSyncProgressChangedEventArgs(
            Guid rootId,
            CottonSyncProgressSnapshot? progress)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            if (progress is not null && progress.RootId != rootId)
            {
                throw new ArgumentException("Sync progress belongs to a different root.", nameof(progress));
            }

            RootId = rootId;
            Progress = progress;
        }

        public Guid RootId { get; }

        public CottonSyncProgressSnapshot? Progress { get; }
    }
}
