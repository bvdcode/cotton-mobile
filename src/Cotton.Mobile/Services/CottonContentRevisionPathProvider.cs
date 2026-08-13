// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonContentRevisionPathProvider : ICottonContentRevisionPathProvider
    {
        public string CreateContentRevisionFilePath(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return CottonMobileStoragePaths.CreateContentRevisionFilePath(instanceUri, root);
        }
    }
}
