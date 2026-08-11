// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonCloudToDeviceFileEntryFactory
    {
        public static CottonFileBrowserEntry Create(CottonCloudToDeviceSyncPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only files can be written by cloud-to-device sync.");
            }

            if (string.IsNullOrWhiteSpace(item.RemoteETag)
                || !item.RemoteUpdatedAtUtc.HasValue
                || item.ContentHash is null)
            {
                throw new InvalidOperationException(
                    "Cloud-to-device file writes require a remote ETag, update time, and content hash.");
            }

            return CottonFileBrowserEntry.CreateFile(
                item.TargetId,
                item.DisplayName,
                item.RemoteUpdatedAtUtc.Value,
                item.SizeBytes,
                item.ContentType,
                previewHashEncryptedHex: null,
                item.RemoteETag,
                contentHash: item.ContentHash);
        }
    }
}
