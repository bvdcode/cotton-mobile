// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCloudToDeviceSyncFileOperator
    {
        Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default);

        Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default);

        Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default);
    }
}
