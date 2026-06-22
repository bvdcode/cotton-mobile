// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class DisabledUserSelectedDocumentTreeCloudToDeviceSyncFileOperator :
        ICottonUserSelectedDocumentTreeCloudToDeviceSyncFileOperator
    {
        public Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("User-selected folder sync is unavailable on this platform.");
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("User-selected folder sync is unavailable on this platform.");
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("User-selected folder sync is unavailable on this platform.");
        }
    }
}
