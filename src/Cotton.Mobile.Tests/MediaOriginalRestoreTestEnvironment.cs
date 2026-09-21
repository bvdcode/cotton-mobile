// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cotton.Mobile.Tests
{
    internal class MediaOriginalRestoreTestEnvironment :
        ICottonDeviceToCloudLocalTreeReader,
        ICottonDeviceToCloudLocalFileContentSource,
        ICottonDeviceToCloudRemoteFolderContentSource,
        ICottonRedactedMediaHashSource,
        IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "cotton-original-media-tests", Guid.NewGuid().ToString("N"));
        private CottonFileBrowserEntry _cloudFile;

        public MediaOriginalRestoreTestEnvironment()
        {
            LocalFile = CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                "photo.jpg", "photo.jpg", DateTime.UnixEpoch, OriginalBytes.Length, "image/jpeg",
                "content://media/external/images/media/1", CottonContentHash.ComputeSha256(OriginalBytes));
            _cloudFile = CreateCloudFile(RedactedHash!);
            Transport.ExpectedETag = $"\"{_cloudFile.ETag}\"";
            foreach (KeyValuePair<string, string> pair in _cloudFile.Metadata)
            {
                Transport.ExistingMetadata.Add(pair.Key, pair.Value);
            }

            _httpClient = new HttpClient(Transport);
            FixedUploadReceiptPathProvider paths = new(_directory);
            Receipts = new FileSystemCottonUploadReceiptStore(paths);
            RestoreStore = new FileSystemCottonMediaOriginalRestoreStore(paths);
            Service = new CottonMediaOriginalRestoreService(
                this, new CottonRecursiveRemoteContentLoader(this), this, RestoreStore, ExecutionLock,
                NullLogger<CottonMediaOriginalRestoreService>.Instance);
            Replacement = new CottonCloudFileReplacement(
                new CottonFileUploadService(new UploadTestClientFactory(_httpClient)), this,
                new CottonSyncFileUploadSourceFactory(this), Receipts, TimeProvider.System,
                NullLogger<CottonCloudFileReplacement>.Instance);
            Executor = new CottonMediaOriginalRestoreExecutor(RestoreStore, this,
                Replacement, new CottonSyncProgressHub(), TimeProvider.System,
                NullLogger<CottonMediaOriginalRestoreExecutor>.Instance);
        }

        public CottonSyncRootSnapshot Root { get; } = new(
            Guid.NewGuid(), new Uri("https://restore.test"), "restore-tests",
            new CottonUploadDestinationSnapshot(Guid.NewGuid(), "Camera", "/Camera"),
            new CottonSyncLocalRootSnapshot(CottonSyncRootStorageKind.MediaStore,
                "content://media/external/file", "Camera", CottonSyncRootPermissionStatus.Available, "camera"),
            CottonSyncDirection.DeviceToCloud, CottonUploadOriginalRetention.KeepOriginals);

        public byte[] OriginalBytes { get; } = "original-media"u8.ToArray();

        public CottonDeviceToCloudLocalItemSnapshot LocalFile { get; }

        public List<CottonDeviceToCloudLocalItemSnapshot> AdditionalLocalFiles { get; } = [];

        public List<CottonFileBrowserEntry> AdditionalCloudFiles { get; } = [];

        public bool WaitForHashCancellation { get; set; }

        public int HashCount { get; private set; }

        public TaskCompletionSource HashStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public UploadHttpMessageHandler Transport { get; } = new();

        public FileSystemCottonUploadReceiptStore Receipts { get; }

        public FileSystemCottonMediaOriginalRestoreStore RestoreStore { get; }

        public CottonMediaOriginalRestoreExecutor Executor { get; }
        public CottonCloudFileReplacement Replacement { get; }

        public CottonSyncRootExecutionLock ExecutionLock { get; } = new();

        public CottonMediaOriginalRestoreService Service { get; }

        public bool IsSupported { get; set; } = true;

        public bool CanReadOriginals { get; set; } = true;

        public string? RedactedHash { get; set; } = CottonContentHash.ComputeSha256("redacted-media"u8);

        public CottonFileBrowserEntry CloudFile
        {
            get => Transport.UpdatedFile is null ? _cloudFile : CottonFileBrowserEntryFactory.FromFile(Transport.UpdatedFile);
            set => _cloudFile = value;
        }

        public CottonDeviceToCloudLocalContentSnapshot LocalContent => new("Camera", [LocalFile, .. AdditionalLocalFiles]);

        public async Task<int> RestoreAsync(CottonMediaOriginalRestorePreview preview, CancellationToken cancellationToken)
        {
            await Service.CompleteReviewAsync(preview, true, cancellationToken);
            return await ExecuteAsync(cancellationToken);
        }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            return ExecutionLock.ExecuteAsync(Root, token => Executor.ApplyAsync(Root, LocalContent,
                new CottonDeviceToCloudRemoteContentSnapshot(Root.CloudFolder.FolderId, "Camera",
                    [new CottonDeviceToCloudRemoteItemSnapshot(CloudFile, CloudFile.Name)]), token), cancellationToken);
        }

        public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri, CottonSyncRootSnapshot root, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(LocalContent);
        }

        public Task<Stream> OpenReadAsync(
            Uri instanceUri, CottonSyncRootSnapshot root, CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream(OriginalBytes, writable: false));
        }

        public Task<CottonFolderContent> LoadAsync(
            Uri instanceUri, CottonFolderHandle folder, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new CottonFolderContent(folder.Id, folder.Name, [CloudFile, .. AdditionalCloudFiles]));
        }

        public async Task<string?> ComputeAsync(
            CottonSyncRootSnapshot root, CottonDeviceToCloudLocalItemSnapshot file,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HashCount++;
            HashStarted.TrySetResult();
            if (WaitForHashCancellation)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return RedactedHash;
        }

        public CottonFileBrowserEntry CreateCloudFile(string hash, string name = "photo.jpg", string? eTag = null, Guid? fileId = null)
        {
            return CottonFileBrowserEntryFactory.CreateFile(
                fileId ?? Guid.NewGuid(), name, DateTime.UnixEpoch, OriginalBytes.Length,
                "image/jpeg", null, eTag ?? "sha256-" + hash,
                new Dictionary<string, string> { ["caption"] = "Keep this caption" }, hash);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            Receipts.Dispose();
            RestoreStore.Dispose();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
