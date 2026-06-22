// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceRemoteItemSnapshot
    {
        public CottonCloudToDeviceRemoteItemSnapshot(CottonFileBrowserEntry entry, string relativePath)
        {
            ArgumentNullException.ThrowIfNull(entry);

            Entry = entry;
            RelativePath = CottonSyncRelativePath.NormalizeFilePath(relativePath, nameof(relativePath));
            if (!string.Equals(
                CottonSyncRelativePath.GetFileName(RelativePath),
                entry.Name,
                StringComparison.Ordinal))
            {
                throw new ArgumentException("Sync relative path file name must match the remote item name.", nameof(relativePath));
            }
        }

        public CottonFileBrowserEntry Entry { get; }

        public string RelativePath { get; }
    }
}
