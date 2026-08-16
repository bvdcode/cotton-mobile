// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncProgressHub
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<Guid, CottonSyncProgressSnapshot> _progressByRootId = [];

        public event EventHandler<CottonSyncProgressChangedEventArgs>? ProgressChanged;

        public void Report(CottonSyncProgressSnapshot progress)
        {
            ArgumentNullException.ThrowIfNull(progress);
            lock (_lock)
            {
                _progressByRootId[progress.RootId] = progress;
            }

            ProgressChanged?.Invoke(
                this,
                new CottonSyncProgressChangedEventArgs(progress.RootId, progress));
        }

        public void Complete(Guid rootId)
        {
            lock (_lock)
            {
                _progressByRootId.Remove(rootId);
            }

            ProgressChanged?.Invoke(
                this,
                new CottonSyncProgressChangedEventArgs(rootId, progress: null));
        }

        public IReadOnlyList<CottonSyncProgressSnapshot> GetCurrent()
        {
            lock (_lock)
            {
                return [.. _progressByRootId.Values];
            }
        }
    }
}
