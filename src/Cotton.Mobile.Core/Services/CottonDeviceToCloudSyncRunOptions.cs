// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncRunOptions
    {
        private CottonDeviceToCloudSyncRunOptions(bool allowDestructiveRemoteDeletes)
        {
            AllowDestructiveRemoteDeletes = allowDestructiveRemoteDeletes;
        }

        public bool AllowDestructiveRemoteDeletes { get; }

        public static CottonDeviceToCloudSyncRunOptions Default { get; } =
            new(allowDestructiveRemoteDeletes: false);

        public static CottonDeviceToCloudSyncRunOptions AllowRemoteDeletes { get; } =
            new(allowDestructiveRemoteDeletes: true);
    }
}
