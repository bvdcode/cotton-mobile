// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonApiTrashPermanentDeleteClient : ICottonTrashPermanentDeleteClient
    {
        private const string IfMatchHeader = "If-Match";

        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonApiTrashPermanentDeleteClient(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task DeleteFileForeverAsync(
            Uri instanceUri,
            Guid fileId,
            string expectedETag,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            if (string.IsNullOrWhiteSpace(expectedETag))
            {
                throw new ArgumentException("Expected file ETag is required.", nameof(expectedETag));
            }

            await _apiClient
                .SendRequiredAsync(
                    instanceUri,
                    HttpMethod.Delete,
                    $"{Routes.V1.Files}/{fileId}?skipTrash=true",
                    new Dictionary<string, string>
                    {
                        [IfMatchHeader] = expectedETag.Trim(),
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteFolderForeverAsync(
            Uri instanceUri,
            Guid folderId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (folderId == Guid.Empty)
            {
                throw new ArgumentException("Folder id is required.", nameof(folderId));
            }

            await _apiClient
                .SendRequiredAsync(
                    instanceUri,
                    HttpMethod.Delete,
                    $"{Routes.V1.Layouts}/nodes/{folderId}?skipTrash=true",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
