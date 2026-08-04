// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class DisabledCottonDeviceToCloudLocalFileOperator : ICottonDeviceToCloudLocalFileOperator
    {
        public Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteIfUnchangedAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CottonDeviceToCloudLocalFileDeleteStatus.Unsupported);
        }
    }
}
