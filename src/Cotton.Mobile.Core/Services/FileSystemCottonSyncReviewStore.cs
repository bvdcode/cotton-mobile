// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncReviewStore(ICottonUploadReceiptPathProvider pathProvider) : IDisposable
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public async Task<CottonSyncReviewState> LoadAsync(
            CottonSyncRootSnapshot root, CancellationToken cancellationToken = default)
        {
            string path = CreatePath(root);
            if (!File.Exists(path))
            {
                return new CottonSyncReviewState(root.StableKey, null, 0, [], [], []);
            }

            CottonSyncReviewState state = await CottonAtomicJsonFile.ReadAsync<CottonSyncReviewState>(
                path, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("Upload review state is missing.");
            string verifiedPath = CreateVerifiedPath(root);
            IReadOnlyList<CottonSyncVerifiedFile> verified = File.Exists(verifiedPath)
                ? await CottonAtomicJsonFile.ReadAsync<CottonSyncVerifiedFile[]>(verifiedPath, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidDataException("Verified upload files are missing.")
                : [];
            state = new CottonSyncReviewState(state.SyncRootStableKey, state.BaselineFingerprint,
                state.UnchangedCount, state.Conflicts, state.Approvals, verified);
            Validate(root, state);
            return state;
        }

        public async Task<CottonSyncReviewState> UpdateAsync(
            CottonSyncRootSnapshot root,
            Func<CottonSyncReviewState, CottonSyncReviewState> update,
            CancellationToken cancellationToken = default)
        {
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CottonSyncReviewState previous = await LoadAsync(root, cancellationToken).ConfigureAwait(false);
                CottonSyncReviewState state = update(previous);
                Validate(root, state);
                if (!ReferenceEquals(previous.VerifiedFiles, state.VerifiedFiles))
                {
                    await CottonAtomicJsonFile.WriteAsync(CreateVerifiedPath(root), state.VerifiedFiles, cancellationToken)
                        .ConfigureAwait(false);
                }
                CottonSyncReviewState review = new(state.SyncRootStableKey, state.BaselineFingerprint,
                    state.UnchangedCount, state.Conflicts, state.Approvals, []);
                await CottonAtomicJsonFile.WriteAsync(CreatePath(root), review, cancellationToken).ConfigureAwait(false);
                return state;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public Task InvalidateAsync(CottonSyncRootSnapshot root, CancellationToken cancellationToken = default)
        {
            return UpdateAsync(root, state => new CottonSyncReviewState(
                root.StableKey, null, 0, state.Conflicts, state.Approvals, state.VerifiedFiles), cancellationToken);
        }

        private string CreatePath(CottonSyncRootSnapshot root)
        {
            return Path.Combine(pathProvider.CreateUploadReceiptDirectory(root.InstanceUri, root),
                "upload-review", "state.json");
        }

        private string CreateVerifiedPath(CottonSyncRootSnapshot root)
        {
            return Path.Combine(pathProvider.CreateUploadReceiptDirectory(root.InstanceUri, root),
                "upload-review", "verified-files.json");
        }

        private static void Validate(CottonSyncRootSnapshot root, CottonSyncReviewState state)
        {
            if (state.SyncRootStableKey != root.StableKey || state.UnchangedCount < 0
                || state.VerifiedFiles.Select(item => item.LocalSourceId).Distinct(StringComparer.Ordinal).Count() != state.VerifiedFiles.Count
                || state.Conflicts.Select(item => item.LocalFile.LocalSourceId).Distinct(StringComparer.Ordinal).Count() != state.Conflicts.Count
                || state.Approvals.Any(item => !item.Conflict.CanReplace || item.OperationId == Guid.Empty)
                || state.Approvals.Select(item => item.Conflict.LocalFile.LocalSourceId).Distinct(StringComparer.Ordinal).Count() != state.Approvals.Count)
            {
                throw new InvalidDataException("Upload review state does not match the selected files.");
            }
        }

        public void Dispose()
        {
            _writeLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
