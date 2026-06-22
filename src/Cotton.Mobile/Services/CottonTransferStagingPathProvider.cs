// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTransferStagingPathProvider : ICottonTransferStagingPathProvider
    {
        public string CreateTransferStagingDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateTransferStagingDirectory(instanceUri);
        }
    }
}
