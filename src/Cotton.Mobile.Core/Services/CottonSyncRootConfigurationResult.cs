// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootConfigurationResult
    {
        public CottonSyncRootConfigurationResult(
            CottonSyncRootConfigurationStatus status,
            CottonSyncRootSnapshot root)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Sync root configuration status is not supported.");
            }

            ArgumentNullException.ThrowIfNull(root);

            Status = status;
            Root = root;
        }

        public CottonSyncRootConfigurationStatus Status { get; }

        public CottonSyncRootSnapshot Root { get; }

        public bool Created => Status == CottonSyncRootConfigurationStatus.Created;

        public bool AlreadyConfigured => Status == CottonSyncRootConfigurationStatus.AlreadyConfigured;

        public bool Updated => Status == CottonSyncRootConfigurationStatus.Updated;
    }
}
