// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileTrashService : ICottonFileTrashService
    {
        private readonly ICottonFileTrashClient _client;

        public CottonFileTrashService(ICottonFileTrashClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
        }

        public async Task<CottonFileTrashMoveResult> MoveFileToTrashAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Only files can be moved to trash.", nameof(file));
            }

            CottonSyncDeleteSemanticsSnapshot semantics = CottonSyncDeleteSemantics.Create(
                file,
                CottonSyncDeleteMode.MoveToTrash);
            if (semantics.SafetyStatus != CottonSyncDeleteSafetyStatus.ConflictSafe
                || string.IsNullOrWhiteSpace(semantics.ExpectedETag))
            {
                throw new InvalidOperationException(CottonFileTrashStatusText.NeedsRefreshStatus);
            }

            await _client.MoveFileToTrashAsync(
                instanceUri,
                file.Id,
                semantics.ExpectedETag,
                cancellationToken).ConfigureAwait(false);
            return CottonFileTrashMoveResult.Moved(file);
        }
    }
}
