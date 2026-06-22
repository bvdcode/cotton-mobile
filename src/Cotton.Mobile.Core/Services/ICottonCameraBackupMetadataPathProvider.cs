// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMetadataPathProvider
    {
        string CreateCameraBackupMetadataDirectory(Uri instanceUri);
    }
}
