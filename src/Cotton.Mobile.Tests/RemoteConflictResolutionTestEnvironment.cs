// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Security.Cryptography;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cotton.Mobile.Tests
{
    internal class RemoteConflictResolutionTestEnvironment :
        ICottonDeviceToCloudLocalTreeReader,
        ICottonDeviceToCloudLocalFileContentSource,
        ICottonDeviceToCloudRemoteFolderContentSource,
        ICottonFileUploadService,
        IDisposable
    {
        private readonly byte[] _localBytes = "local replacement"u8.ToArray();
        private readonly List<CottonDeviceToCloudLocalItemSnapshot> _additionalLocal = [];
        private readonly Dictionary<Guid, CottonFileBrowserEntry> _additionalCloud = [];
        private readonly string _directory = Path.Combine(
            Path.GetTempPath(),
            "cotton-conflict-resolution-tests",
            Guid.NewGuid().ToString("N"));

        public RemoteConflictResolutionTestEnvironment()
        {
            string localHash = Convert.ToHexStringLower(SHA256.HashData(_localBytes));
            LocalFile = CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                "photo.jpg",
                "photo.jpg",
                DateTime.UnixEpoch,
                _localBytes.Length,
                "image/jpeg",
                "content://documents/photo.jpg",
                localHash);
            CloudFile = CottonFileBrowserEntryFactory.CreateFile(
                Guid.NewGuid(),
                "photo.jpg",
                DateTime.UnixEpoch,
                14,
                "image/jpeg",
                previewHashEncryptedHex: null,
                "cloud-etag-1",
                contentHash: TestContentHashes.Second);
            FixedUploadReceiptPathProvider paths = new(_directory);
            Receipts = new FileSystemCottonUploadReceiptStore(paths);
            ReviewStore = new FileSystemCottonSyncReviewStore(paths);
            Replacement = new CottonCloudFileReplacement(this, this,
                new CottonSyncFileUploadSourceFactory(this), Receipts, TimeProvider.System,
                NullLogger<CottonCloudFileReplacement>.Instance);
            Service = new CottonRemoteConflictResolutionService(
                this,
                new CottonRecursiveRemoteContentLoader(this),
                Receipts,
                ReviewStore,
                Replacement,
                ExecutionLock,
                new CottonSyncProgressHub(),
                TimeProvider.System,
                NullLogger<CottonRemoteConflictResolutionService>.Instance);
        }

        public CottonSyncRootSnapshot Root { get; } = DeviceToCloudSyncPlannerTestData.CreateReadyRoot();

        public CottonDeviceToCloudLocalItemSnapshot LocalFile { get; }

        public CottonFileBrowserEntry CloudFile { get; private set; }

        public FileSystemCottonUploadReceiptStore Receipts { get; }

        public CottonRemoteConflictResolutionService Service { get; }
        public FileSystemCottonSyncReviewStore ReviewStore { get; }
        public CottonCloudFileReplacement Replacement { get; }
        public CottonSyncRootExecutionLock ExecutionLock { get; } = new();

        public async Task<int> ApproveAndApplyAsync(CancellationToken cancellationToken)
        {
            CottonSyncReviewState review = await Service.ScanAsync(Root, cancellationToken);
            await Service.ApproveAsync(Root, [.. review.Conflicts.Where(item => item.CanReplace)], cancellationToken);
            return await ApplyAsync(cancellationToken);
        }

        public async Task<int> ApplyAsync(CancellationToken cancellationToken)
        {
            CottonDeviceToCloudLocalContentSnapshot local = await ReadAsync(Root.InstanceUri, Root, cancellationToken);
            CottonDeviceToCloudRemoteContentSnapshot remote = await new CottonRecursiveRemoteContentLoader(this)
                .LoadAsync(Root.InstanceUri, Root, cancellationToken);
            return await Service.ApplyAsync(Root, local, remote, cancellationToken);
        }

        public string? ReceivedExpectedETag { get; private set; }

        public bool LoseUpdateResponse { get; set; }
        public bool FailBeforeUpdate { get; set; }
        public bool InterruptAfterUpdate { get; set; }
        public int UpdateCount { get; private set; }
        public int? FailAfterUpdates { get; set; }

        public void AddFiles(int count)
        {
            for (int index = 0; index < count; index++)
            {
                string name = $"extra-{index}.jpg";
                _additionalLocal.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFile(name, name,
                    DateTime.UnixEpoch, _localBytes.Length, "image/jpeg", $"document:{index}", LocalFile.ContentHash));
                Guid id = Guid.NewGuid();
                _additionalCloud.Add(id, CottonFileBrowserEntryFactory.CreateFile(id, name,
                    DateTime.UnixEpoch, 14, "image/jpeg", null, $"etag-{index}", contentHash: TestContentHashes.Second));
            }
        }

        public void ChangeCloudRevision()
        {
            CloudFile = CottonFileBrowserEntryFactory.CreateFile(CloudFile.Id, CloudFile.Name,
                DateTime.UnixEpoch.AddDays(1), CloudFile.SizeBytes, CloudFile.ContentType,
                null, "changed-etag", contentHash: TestContentHashes.Second);
        }

        public void UseFolderConflict()
        {
            CloudFile = CottonFileBrowserEntryFactory.CreateFolder(
                Guid.NewGuid(),
                LocalFile.DisplayName,
                DateTime.UnixEpoch);
        }

        public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CottonDeviceToCloudLocalContentSnapshot("Photos", [LocalFile, .. _additionalLocal]));
        }

        public Task<Stream> OpenReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream(_localBytes, writable: false));
        }

        public Task<CottonFolderContent> LoadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CottonFolderContent(folder.Id, folder.Name,
                folder.Id == Root.CloudFolder.FolderId ? [CloudFile, .. _additionalCloud.Values] : []));
        }

        public Task<CottonFileBrowserEntry> UploadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CottonFileUploadSource source,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async Task<CottonFileBrowserEntry> UpdateContentAsync(
            Uri instanceUri,
            Guid fileId,
            CottonFolderHandle folder,
            string expectedETag,
            CottonFileUploadSource source,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (FailBeforeUpdate || UpdateCount == FailAfterUpdates)
            {
                throw new HttpRequestException("Connection interrupted before replacement.");
            }

            UpdateCount++;
            ReceivedExpectedETag = expectedETag;
            await using Stream content = await source.OpenReadAsync(cancellationToken);
            using MemoryStream copy = new();
            await content.CopyToAsync(copy, cancellationToken);
            byte[] bytes = copy.ToArray();
            string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            progress?.Report(bytes.Length);
            CottonFileBrowserEntry updated = CottonFileBrowserEntryFactory.CreateFile(
                fileId,
                source.Snapshot.Name,
                DateTime.UnixEpoch.AddMinutes(1),
                bytes.Length,
                source.Snapshot.ContentType,
                previewHashEncryptedHex: null,
                "cloud-etag-2",
                contentHash: hash);
            if (fileId == CloudFile.Id)
            {
                CloudFile = updated;
            }
            else
            {
                _additionalCloud[fileId] = updated;
            }
            if (InterruptAfterUpdate)
            {
                throw new OperationCanceledException("The process stopped before recording the result.");
            }

            if (LoseUpdateResponse)
            {
                throw new HttpRequestException("The update response was lost.");
            }

            return updated;
        }

        public void Dispose()
        {
            Receipts.Dispose();
            ReviewStore.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
