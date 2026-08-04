// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonUploadReceiptPathProvider : ICottonUploadReceiptPathProvider
    {
        public string CreateUploadReceiptDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return CottonMobileStoragePaths.CreateUploadReceiptDirectory(instanceUri, root);
        }
    }
}
