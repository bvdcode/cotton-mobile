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
            Receipts = new FileSystemCottonUploadReceiptStore(
                new FixedUploadReceiptPathProvider(_directory));
            Service = new CottonRemoteConflictResolutionService(
                this,
                new CottonRecursiveRemoteContentLoader(this),
                Receipts,
                this,
                new CottonSyncFileUploadSourceFactory(this),
                new CottonSyncProgressHub(),
                TimeProvider.System,
                NullLogger<CottonRemoteConflictResolutionService>.Instance);
        }

        public CottonSyncRootSnapshot Root { get; } = DeviceToCloudSyncPlannerTestData.CreateReadyRoot();

        public CottonDeviceToCloudLocalItemSnapshot LocalFile { get; }

        public CottonFileBrowserEntry CloudFile { get; private set; }

        public FileSystemCottonUploadReceiptStore Receipts { get; }

        public CottonRemoteConflictResolutionService Service { get; }

        public string? ReceivedExpectedETag { get; private set; }

        public bool LoseUpdateResponse { get; set; }

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
            return Task.FromResult(new CottonDeviceToCloudLocalContentSnapshot("Photos", [LocalFile]));
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
            return Task.FromResult(new CottonFolderContent(folder.Id, folder.Name, [CloudFile]));
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
            ReceivedExpectedETag = expectedETag;
            await using Stream content = await source.OpenReadAsync(cancellationToken);
            using MemoryStream copy = new();
            await content.CopyToAsync(copy, cancellationToken);
            byte[] bytes = copy.ToArray();
            string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            progress?.Report(bytes.Length);
            CloudFile = CottonFileBrowserEntryFactory.CreateFile(
                fileId,
                source.Snapshot.Name,
                DateTime.UnixEpoch.AddMinutes(1),
                bytes.Length,
                source.Snapshot.ContentType,
                previewHashEncryptedHex: null,
                "cloud-etag-2",
                contentHash: hash);
            if (LoseUpdateResponse)
            {
                throw new HttpRequestException("The update response was lost.");
            }

            return CloudFile;
        }

        public void Dispose()
        {
            Receipts.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
