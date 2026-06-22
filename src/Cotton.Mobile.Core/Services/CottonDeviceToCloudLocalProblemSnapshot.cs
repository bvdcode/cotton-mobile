// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudLocalProblemSnapshot
    {
        public CottonDeviceToCloudLocalProblemSnapshot(
            CottonDeviceToCloudLocalProblemKind kind,
            CottonFileBrowserEntryType itemType,
            string displayName,
            string relativePath,
            string detailText)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Device-to-cloud local problem kind is not supported.");
            }

            if (!Enum.IsDefined(itemType))
            {
                throw new ArgumentOutOfRangeException(nameof(itemType), "Device-to-cloud local problem item type is not supported.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Device-to-cloud local problem display name is required.", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Device-to-cloud local problem relative path is required.", nameof(relativePath));
            }

            if (string.IsNullOrWhiteSpace(detailText))
            {
                throw new ArgumentException("Device-to-cloud local problem detail is required.", nameof(detailText));
            }

            Kind = kind;
            ItemType = itemType;
            DisplayName = displayName.Trim();
            RelativePath = relativePath.Trim();
            DetailText = detailText.Trim();
        }

        public CottonDeviceToCloudLocalProblemKind Kind { get; }

        public CottonFileBrowserEntryType ItemType { get; }

        public string DisplayName { get; }

        public string RelativePath { get; }

        public string DetailText { get; }
    }
}
