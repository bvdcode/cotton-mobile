// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonOfflineFileMetadataPathProvider : ICottonOfflineFileMetadataPathProvider
    {
        public string CreateOfflineFileMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateOfflineFileMetadataDirectory(instanceUri);
        }
    }
}
