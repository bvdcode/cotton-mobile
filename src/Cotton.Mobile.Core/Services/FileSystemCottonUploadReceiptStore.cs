// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Security.Cryptography;
using System.Text;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonUploadReceiptStore : ICottonUploadReceiptStore, IDisposable
    {
        private const int SchemaVersion = 1;
        private const string ReceiptFileExtension = ".json";
        private readonly ICottonUploadReceiptPathProvider _pathProvider;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public FileSystemCottonUploadReceiptStore(ICottonUploadReceiptPathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

        public async Task<IReadOnlyList<CottonUploadReceiptSnapshot>> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            EnsureSupportedRoot(instanceUri, root);

            string directory = _pathProvider.CreateUploadReceiptDirectory(instanceUri, root);
            if (!Directory.Exists(directory))
            {
                return [];
            }

            string[] filePaths = Directory.GetFiles(directory, $"*{ReceiptFileExtension}", SearchOption.TopDirectoryOnly);
            Array.Sort(filePaths, StringComparer.Ordinal);
            Dictionary<string, CottonUploadReceiptSnapshot> receiptsBySourceId = new(StringComparer.Ordinal);
            foreach (string filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CottonUploadReceiptSnapshot receipt = await LoadReceiptAsync(
                    filePath,
                    root,
                    cancellationToken).ConfigureAwait(false);
                string expectedFileName = CreateReceiptFileName(receipt.LocalSourceId);
                if (!string.Equals(Path.GetFileName(filePath), expectedFileName, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Upload receipt file name does not match its local source id.");
                }

                if (!receiptsBySourceId.TryAdd(receipt.LocalSourceId, receipt))
                {
                    throw new InvalidDataException("Upload receipt store contains a duplicate local source id.");
                }
            }

            return [.. receiptsBySourceId.Values];
        }

        public async Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonUploadReceiptSnapshot receipt,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(receipt);
            EnsureSupportedRoot(instanceUri, root);

            string directory = _pathProvider.CreateUploadReceiptDirectory(instanceUri, root);
            string filePath = Path.Combine(directory, CreateReceiptFileName(receipt.LocalSourceId));
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (File.Exists(filePath))
                {
                    CottonUploadReceiptSnapshot previous = await LoadReceiptAsync(
                        filePath,
                        root,
                        cancellationToken).ConfigureAwait(false);
                    ValidateTransition(previous, receipt);
                }
                else if (!receipt.IsPending)
                {
                    throw new InvalidDataException("Upload receipt history must begin in the pending state.");
                }

                await CottonAtomicJsonFile
                    .WriteAsync(filePath, CreateStoredReceipt(root, receipt), cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            EnsureSupportedRoot(instanceUri, root);
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string directory = _pathProvider.CreateUploadReceiptDirectory(instanceUri, root);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
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

        private static async Task<CottonUploadReceiptSnapshot> LoadReceiptAsync(
            string filePath,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            CottonStoredUploadReceipt? stored = await CottonAtomicJsonFile
                .ReadAsync<CottonStoredUploadReceipt>(filePath, cancellationToken)
                .ConfigureAwait(false);
            if (stored is null
                || stored.SchemaVersion != SchemaVersion
                || !string.Equals(stored.SyncRootStableKey, root.StableKey, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Upload receipt metadata is invalid for this sync root.");
            }

            try
            {
                return new CottonUploadReceiptSnapshot(
                    stored.LocalSourceId ?? string.Empty,
                    stored.RelativePath ?? string.Empty,
                    stored.LocalUpdatedAtUtc,
                    stored.SizeBytes,
                    stored.ContentType,
                    stored.OperationId,
                    stored.Status,
                    stored.RecordedAtUtc,
                    stored.RemoteFileId,
                    stored.RemoteETag,
                    stored.ContentHash);
            }
            catch (Exception exception)
                when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                throw new InvalidDataException("Upload receipt contains invalid data.", exception);
            }
        }

        private static CottonStoredUploadReceipt CreateStoredReceipt(
            CottonSyncRootSnapshot root,
            CottonUploadReceiptSnapshot receipt)
        {
            return new CottonStoredUploadReceipt
            {
                SchemaVersion = SchemaVersion,
                SyncRootStableKey = root.StableKey,
                LocalSourceId = receipt.LocalSourceId,
                RelativePath = receipt.RelativePath,
                LocalUpdatedAtUtc = receipt.LocalUpdatedAtUtc,
                SizeBytes = receipt.SizeBytes,
                ContentType = receipt.ContentType,
                OperationId = receipt.OperationId,
                Status = receipt.Status,
                RecordedAtUtc = receipt.RecordedAtUtc,
                RemoteFileId = receipt.RemoteFileId,
                RemoteETag = receipt.RemoteETag,
                ContentHash = receipt.ContentHash,
            };
        }

        private static void ValidateTransition(
            CottonUploadReceiptSnapshot previous,
            CottonUploadReceiptSnapshot next)
        {
            if (!previous.IsPending || !next.IsUploaded)
            {
                throw new InvalidDataException("Uploaded receipts cannot be replaced or downgraded.");
            }

            if (!string.Equals(previous.LocalSourceId, next.LocalSourceId, StringComparison.Ordinal)
                || !string.Equals(previous.RelativePath, next.RelativePath, StringComparison.Ordinal)
                || previous.LocalUpdatedAtUtc != next.LocalUpdatedAtUtc
                || previous.SizeBytes != next.SizeBytes
                || !string.Equals(previous.ContentType, next.ContentType, StringComparison.Ordinal)
                || !string.Equals(previous.ContentHash, next.ContentHash, StringComparison.Ordinal)
                || previous.OperationId != next.OperationId)
            {
                throw new InvalidDataException("Upload receipt identity changed during confirmation.");
            }
        }

        private static string CreateReceiptFileName(string localSourceId)
        {
            byte[] source = Encoding.UTF8.GetBytes(localSourceId);
            string hash = Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
            return $"{hash}{ReceiptFileExtension}";
        }

        private static void EnsureSupportedRoot(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            if (!Uri.Equals(instanceUri, root.InstanceUri))
            {
                throw new ArgumentException("Upload receipt instance does not match the sync root.", nameof(instanceUri));
            }

            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new ArgumentException("Upload receipts require an upload-only sync root.", nameof(root));
            }
        }
    }
}
