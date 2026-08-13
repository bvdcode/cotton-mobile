// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserRemoteFolderContentSource(
        ICottonFileBrowserService fileBrowserService) :
        ICottonDeviceToCloudRemoteFolderContentSource
    {
        private readonly ICottonFileBrowserService _fileBrowserService =
            fileBrowserService ?? throw new ArgumentNullException(nameof(fileBrowserService));

        public Task<CottonFolderContent> LoadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            return _fileBrowserService.GetFolderAsync(instanceUri, folder, cancellationToken);
        }
    }
}
