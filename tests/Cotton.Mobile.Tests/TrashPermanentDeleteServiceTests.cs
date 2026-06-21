using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashPermanentDeleteServiceTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid FileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid FolderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime UpdatedAt = new(2026, 6, 21, 13, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Delete_file_forever_uses_expected_etag_and_skips_server_trash()
        {
            var client = new FakeTrashPermanentDeleteClient();
            var service = new CottonTrashPermanentDeleteService(client);

            CottonTrashPermanentDeleteResult result = await service.DeleteForeverAsync(
                InstanceUri,
                CreateFile("\"etag-1\""));

            Assert.Equal(FileId, result.ItemId);
            Assert.Equal(CottonFileBrowserEntryType.File, result.ItemType);
            Assert.Equal("notes.txt permanently deleted.", result.StatusText);
            Assert.Equal(InstanceUri, client.FileInstanceUri);
            Assert.Equal(FileId, client.FileId);
            Assert.Equal("\"etag-1\"", client.ExpectedETag);
            Assert.Null(client.FolderId);
        }

        [Fact]
        public async Task Delete_file_forever_requires_fresh_file_etag()
        {
            var client = new FakeTrashPermanentDeleteClient();
            var service = new CottonTrashPermanentDeleteService(client);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.DeleteForeverAsync(InstanceUri, CreateFile(eTag: null)));

            Assert.Equal(CottonTrashPermanentDeleteStatusText.NeedsRefreshStatus, exception.Message);
            Assert.Null(client.FileId);
            Assert.Null(client.FolderId);
        }

        [Fact]
        public async Task Delete_folder_forever_skips_server_trash_without_etag()
        {
            var client = new FakeTrashPermanentDeleteClient();
            var service = new CottonTrashPermanentDeleteService(client);

            CottonTrashPermanentDeleteResult result = await service.DeleteForeverAsync(
                InstanceUri,
                CreateFolder());

            Assert.Equal(FolderId, result.ItemId);
            Assert.Equal(CottonFileBrowserEntryType.Folder, result.ItemType);
            Assert.Equal("Archive permanently deleted.", result.StatusText);
            Assert.Equal(InstanceUri, client.FolderInstanceUri);
            Assert.Equal(FolderId, client.FolderId);
            Assert.Null(client.FileId);
        }

        [Fact]
        public void Delete_forever_status_text_is_explicit()
        {
            Assert.Equal(
                "Permanently delete notes.txt? This cannot be undone.",
                CottonTrashPermanentDeleteStatusText.CreateConfirmMessage(
                    " notes.txt ",
                    CottonFileBrowserEntryType.File));
            Assert.Equal(
                "Permanently delete Archive and its contents? This cannot be undone.",
                CottonTrashPermanentDeleteStatusText.CreateConfirmMessage(
                    "Archive",
                    CottonFileBrowserEntryType.Folder));
            Assert.Equal(
                "Deleting notes.txt forever...",
                CottonTrashPermanentDeleteStatusText.CreateDeletingStatus("notes.txt"));
            Assert.Equal(
                "notes.txt permanently deleted.",
                CottonTrashPermanentDeleteStatusText.CreateDeletedStatus("notes.txt"));
            Assert.Equal(
                "notes.txt permanently deleted. Refresh to update trash.",
                CottonTrashPermanentDeleteStatusText.CreateDeletedNeedsRefreshStatus("notes.txt"));
            Assert.Equal("Delete forever cancelled.", CottonTrashPermanentDeleteStatusText.CancelledStatus);
            Assert.Equal(
                "Could not permanently delete item.",
                CottonTrashPermanentDeleteStatusText.FailedStatus);
            Assert.Equal(
                "Offline. Delete forever needs internet.",
                CottonTrashPermanentDeleteStatusText.OfflineUnavailableStatus);
        }

        private static CottonFileBrowserEntry CreateFile(string? eTag)
        {
            return CottonFileBrowserEntry.CreateCached(
                FileId,
                CottonFileBrowserEntryType.File,
                "notes.txt",
                "Text",
                "42 B · Text",
                "More",
                "TXT",
                UpdatedAt,
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag);
        }

        private static CottonFileBrowserEntry CreateFolder()
        {
            return CottonFileBrowserEntry.CreateCached(
                FolderId,
                CottonFileBrowserEntryType.Folder,
                "Archive",
                "Folder",
                "Folder",
                "Open",
                "Folder",
                UpdatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null,
                eTag: null);
        }

        private class FakeTrashPermanentDeleteClient : ICottonTrashPermanentDeleteClient
        {
            public Uri? FileInstanceUri { get; private set; }

            public Guid? FileId { get; private set; }

            public string? ExpectedETag { get; private set; }

            public Uri? FolderInstanceUri { get; private set; }

            public Guid? FolderId { get; private set; }

            public Task DeleteFileForeverAsync(
                Uri instanceUri,
                Guid fileId,
                string expectedETag,
                CancellationToken cancellationToken = default)
            {
                FileInstanceUri = instanceUri;
                FileId = fileId;
                ExpectedETag = expectedETag;
                return Task.CompletedTask;
            }

            public Task DeleteFolderForeverAsync(
                Uri instanceUri,
                Guid folderId,
                CancellationToken cancellationToken = default)
            {
                FolderInstanceUri = instanceUri;
                FolderId = folderId;
                return Task.CompletedTask;
            }
        }
    }
}
