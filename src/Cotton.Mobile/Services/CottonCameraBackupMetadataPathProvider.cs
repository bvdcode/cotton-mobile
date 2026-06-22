// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupMetadataPathProvider : ICottonCameraBackupMetadataPathProvider
    {
        public string CreateCameraBackupMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateCameraBackupMetadataDirectory(instanceUri);
        }
    }
}
