using Cotton.Mobile.Services;
using static Cotton.Mobile.Tests.UploadOnlySyncPlanExecutorTestData;

namespace Cotton.Mobile.Tests
{
    internal class UploadOnlySyncFileOperator(List<string> events) : ICottonDeviceToCloudSyncFileOperator
    {
        public List<CottonDeviceToCloudSyncPlanItem> UploadCalls { get; } = [];

        public Exception? UploadException { get; set; }

        public Task<CottonFileBrowserEntry> UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            UploadCalls.Add(item);
            events.Add("remote:upload");
            if (UploadException is not null)
            {
                return Task.FromException<CottonFileBrowserEntry>(UploadException);
            }

            Guid operationId = item.UploadOperationId
                ?? throw new InvalidOperationException("Upload call requires an operation id.");
            IReadOnlyDictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CottonFileUploadMetadataKeys.UploadOperationId] = operationId.ToString("N"),
            };
            CottonFileBrowserEntry uploaded = CottonFileBrowserEntryFactory.CreateFile(
                RemoteFileId,
                item.DisplayName,
                RecordedAt,
                item.SizeBytes,
                item.ContentType,
                previewHashEncryptedHex: null,
                eTag: "etag-remote",
                metadata,
                TestContentHashes.First);
            return Task.FromResult(uploaded);
        }

        public Task<CottonFileBrowserEntry> CreateFolderAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFolderHandle parentFolder,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Folder creation was not expected.");
        }

    }
}
