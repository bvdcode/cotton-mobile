// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public class CottonTrashRestoreService : ICottonTrashRestoreService
    {
        private readonly ICottonTrashRestoreClient _client;

        public CottonTrashRestoreService(ICottonTrashRestoreClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
        }

        public async Task<CottonTrashRestoreResult> RestoreAsync(
            Uri instanceUri,
            Guid itemId,
            CottonFileBrowserEntryType itemType,
            CottonTrashRestoreRetryMode retryMode = CottonTrashRestoreRetryMode.None,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ValidateItemId(itemId);
            ValidateItemType(itemType);
            ValidateRetryMode(retryMode);

            RestoreItemRequestDto request = CreateRequest(retryMode);
            RestoreOutcomeDto outcome = itemType == CottonFileBrowserEntryType.File
                ? await _client.RestoreFileAsync(instanceUri, itemId, request, cancellationToken).ConfigureAwait(false)
                : await _client.RestoreFolderAsync(instanceUri, itemId, request, cancellationToken).ConfigureAwait(false);

            return CottonTrashRestoreResult.Create(itemId, itemType, retryMode, outcome);
        }

        private static RestoreItemRequestDto CreateRequest(CottonTrashRestoreRetryMode retryMode)
        {
            RestoreItemRequestDto request = CottonSyncRestorePolicy.CreateDefaultRequest();
            if (retryMode == CottonTrashRestoreRetryMode.CreateMissingParents)
            {
                request.CreateMissingParents = true;
            }
            else if (retryMode == CottonTrashRestoreRetryMode.Overwrite)
            {
                request.Overwrite = true;
            }

            return request;
        }

        private static void ValidateItemId(Guid itemId)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Restore item id is required.", nameof(itemId));
            }
        }

        private static void ValidateItemType(CottonFileBrowserEntryType itemType)
        {
            if (itemType is not CottonFileBrowserEntryType.File and not CottonFileBrowserEntryType.Folder)
            {
                throw new ArgumentOutOfRangeException(nameof(itemType), "Restore item type is not supported.");
            }
        }

        private static void ValidateRetryMode(CottonTrashRestoreRetryMode retryMode)
        {
            if (!Enum.IsDefined(retryMode))
            {
                throw new ArgumentOutOfRangeException(nameof(retryMode), "Restore retry mode is not supported.");
            }
        }
    }
}
