// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAppPrivateCloudToDeviceSyncFileOperator : ICottonCloudToDeviceSyncFileOperator
    {
        private readonly ICottonFileBrowserService _fileBrowserService;

        public CottonAppPrivateCloudToDeviceSyncFileOperator(ICottonFileBrowserService fileBrowserService)
        {
            ArgumentNullException.ThrowIfNull(fileBrowserService);

            _fileBrowserService = fileBrowserService;
        }

        public async Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);
            CottonFileBrowserEntry file = CreateFileEntry(item);
            await _fileBrowserService
                .DownloadAsync(instanceUri, file, progress: null, cancellationToken)
                .ConfigureAwait(false);
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);
            CottonFileBrowserEntry file = CreateFileEntry(item);
            return Task.Run(
                () => RenameLocalDownload(instanceUri, file, item, cancellationToken),
                cancellationToken);
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);
            return Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string directory = CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, item.TargetId);
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                },
                cancellationToken);
        }

        private static void EnsureSupportedRoot(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);

            if (!string.Equals(
                CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri),
                CottonMobileStoragePaths.CreateInstanceStorageKey(root.InstanceUri),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Cloud-to-device sync instance does not match the sync root.");
            }

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Cloud-to-device sync root is not ready.");
            }

            if (!root.LocalRoot.UsesAppPrivateStorage)
            {
                throw new InvalidOperationException("This sync file operator only supports app-private local roots.");
            }

            if (root.Direction == CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("This sync file operator requires cloud-to-device sync direction.");
            }
        }

        private static CottonFileBrowserEntry CreateFileEntry(CottonCloudToDeviceSyncPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only files can be written by cloud-to-device sync.");
            }

            if (string.IsNullOrWhiteSpace(item.RemoteETag) || !item.RemoteUpdatedAtUtc.HasValue)
            {
                throw new InvalidOperationException("Cloud-to-device file writes require a remote ETag and update time.");
            }

            return CottonFileBrowserEntry.CreateFile(
                item.TargetId,
                item.DisplayName,
                item.RemoteUpdatedAtUtc.Value,
                item.SizeBytes,
                item.ContentType,
                previewHashEncryptedHex: null,
                item.RemoteETag);
        }

        private static void RenameLocalDownload(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string directory = CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, file);
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Synced local file directory was not found for {file.Id}.");
            }

            string targetPath = CottonMobileStoragePaths.CreateDownloadPath(instanceUri, file);
            FileInfo[] localFiles = Directory
                .EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => !CottonMobileStoragePaths.IsTemporaryDownloadPath(path))
                .Select(path => new FileInfo(path))
                .Where(info => info.Exists)
                .ToArray();
            FileInfo? targetFile = localFiles
                .FirstOrDefault(info => string.Equals(info.FullName, targetPath, StringComparison.Ordinal));

            if (targetFile is not null)
            {
                if (localFiles.Length > 1)
                {
                    throw new IOException($"Synced local file directory is ambiguous for {file.Id}.");
                }

                ValidateLocalFileSize(targetFile, item);
                StampLocalFile(targetFile.FullName, item);
                return;
            }

            if (localFiles.Length == 0)
            {
                throw new FileNotFoundException("Synced local file was not found.", targetPath);
            }

            if (localFiles.Length > 1)
            {
                throw new IOException($"Synced local file directory is ambiguous for {file.Id}.");
            }

            FileInfo sourceFile = localFiles[0];
            ValidateLocalFileSize(sourceFile, item);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(sourceFile.FullName, targetPath, overwrite: true);
            StampLocalFile(targetPath, item);
        }

        private static void ValidateLocalFileSize(FileInfo file, CottonCloudToDeviceSyncPlanItem item)
        {
            if (item.SizeBytes.HasValue && file.Length != item.SizeBytes.Value)
            {
                throw new IOException(
                    $"Synced local file size mismatch for {item.TargetId}: expected {item.SizeBytes.Value} bytes, got {file.Length} bytes.");
            }
        }

        private static void StampLocalFile(string path, CottonCloudToDeviceSyncPlanItem item)
        {
            if (!item.RemoteUpdatedAtUtc.HasValue)
            {
                throw new InvalidOperationException("Synced local file timestamp is required.");
            }

            File.SetLastWriteTimeUtc(path, CottonLocalFileFreshness.NormalizeUtc(item.RemoteUpdatedAtUtc.Value));
        }
    }
}
