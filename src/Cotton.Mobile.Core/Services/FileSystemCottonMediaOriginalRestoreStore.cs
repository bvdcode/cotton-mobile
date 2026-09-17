// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonMediaOriginalRestoreStore(ICottonUploadReceiptPathProvider pathProvider)
        : ICottonMediaOriginalRestoreStore, IDisposable
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public async Task<CottonMediaOriginalRestoreState> LoadAsync(
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = CreatePath(root);
            if (!File.Exists(path))
            {
                return new CottonMediaOriginalRestoreState(root.StableKey, false, []);
            }

            CottonMediaOriginalRestoreState state = await CottonAtomicJsonFile
                .ReadAsync<CottonMediaOriginalRestoreState>(path, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidDataException("Original media recovery state is missing.");
            Validate(root, state);
            return state;
        }

        public async Task<CottonMediaOriginalRestoreState> UpdateAsync(
            CottonSyncRootSnapshot root,
            Func<CottonMediaOriginalRestoreState, CottonMediaOriginalRestoreState> update,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(update);
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CottonMediaOriginalRestoreState previous = await LoadAsync(root, cancellationToken).ConfigureAwait(false);
                CottonMediaOriginalRestoreState state = update(previous);
                Validate(root, state);
                await CottonAtomicJsonFile.WriteAsync(CreatePath(root), state, cancellationToken).ConfigureAwait(false);
                return state;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            _writeLock.Dispose();
            GC.SuppressFinalize(this);
        }

        private string CreatePath(CottonSyncRootSnapshot root)
        {
            return Path.Combine(pathProvider.CreateUploadReceiptDirectory(root.InstanceUri, root),
                "original-media-recovery", "state.json");
        }

        private static void Validate(CottonSyncRootSnapshot root, CottonMediaOriginalRestoreState state)
        {
            if (!string.Equals(root.StableKey, state.SyncRootStableKey, StringComparison.Ordinal)
                || state.Approvals.Select(item => item.LocalFile.LocalSourceId).Distinct(StringComparer.Ordinal).Count() != state.Approvals.Count)
            {
                throw new InvalidDataException("Original media recovery state does not match the upload folder.");
            }
        }
    }
}
