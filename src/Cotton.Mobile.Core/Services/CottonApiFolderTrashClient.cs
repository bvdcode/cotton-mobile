// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonApiFolderTrashClient : ICottonFolderTrashClient
    {
        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonApiFolderTrashClient(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task MoveFolderToTrashAsync(
            Uri instanceUri,
            Guid folderId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Folder id is required.", nameof(folderId));
            }

            string path = $"{Routes.V1.Layouts}/nodes/{folderId}?skipTrash=false";
            await _apiClient
                .SendRequiredAsync(instanceUri, HttpMethod.Delete, path, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
