// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootMetadataPathProvider : ICottonSyncRootMetadataPathProvider
    {
        public string CreateSyncRootMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateSyncRootMetadataDirectory(instanceUri);
        }
    }
}
