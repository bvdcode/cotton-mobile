// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncVerifiedFile(string localSourceId, string relativePath, long? sizeBytes, string contentHash)
    {
        public string LocalSourceId { get; } = localSourceId;
        public string RelativePath { get; } = relativePath;
        public long? SizeBytes { get; } = sizeBytes;
        public string ContentHash { get; } = contentHash;

        public bool Matches(CottonDeviceToCloudLocalItemSnapshot file)
        {
            return LocalSourceId == file.LocalSourceId && RelativePath == file.RelativePath
                && SizeBytes == file.SizeBytes && ContentHash == file.ContentHash;
        }
    }
}
