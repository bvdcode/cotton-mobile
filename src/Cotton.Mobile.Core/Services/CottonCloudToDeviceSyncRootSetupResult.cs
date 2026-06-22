// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncRootSetupResult
    {
        public CottonCloudToDeviceSyncRootSetupResult(
            CottonCloudToDeviceSyncRootSetupStatus status,
            CottonSyncRootSnapshot root)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Sync root setup status is not supported.");
            }

            ArgumentNullException.ThrowIfNull(root);

            Status = status;
            Root = root;
        }

        public CottonCloudToDeviceSyncRootSetupStatus Status { get; }

        public CottonSyncRootSnapshot Root { get; }

        public bool Created => Status == CottonCloudToDeviceSyncRootSetupStatus.Created;

        public bool AlreadyConfigured => Status == CottonCloudToDeviceSyncRootSetupStatus.AlreadyConfigured;

        public bool Updated => Status == CottonCloudToDeviceSyncRootSetupStatus.Updated;
    }
}
