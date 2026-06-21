using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FolderTrashServiceTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid FolderId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Move_folder_to_trash_uses_server_trash()
        {
            var client = new FakeFolderTrashClient();
            var service = new CottonFolderTrashService(client);
            CottonFileBrowserEntry folder = CreateFolder();

            CottonFolderTrashMoveResult result = await service.MoveFolderToTrashAsync(InstanceUri, folder);

            Assert.Equal(FolderId, result.FolderId);
            Assert.Equal("Archive", result.FolderName);
            Assert.Equal("Archive moved to trash.", result.StatusText);
            Assert.Equal(InstanceUri, client.InstanceUri);
            Assert.Equal(FolderId, client.FolderId);
        }

        [Fact]
        public async Task Move_folder_to_trash_rejects_files()
        {
            var client = new FakeFolderTrashClient();
            var service = new CottonFolderTrashService(client);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.MoveFolderToTrashAsync(InstanceUri, CreateFile()));
            Assert.Null(client.FolderId);
        }

        [Fact]
        public void Folder_trash_status_text_is_explicit()
        {
            Assert.Equal(
                "Archive and its contents will be removed from this folder and can be restored from trash.",
                CottonFolderTrashStatusText.CreateConfirmMessage(" Archive "));
            Assert.Equal("Moving Archive to trash...", CottonFolderTrashStatusText.CreateMovingStatus("Archive"));
            Assert.Equal("Archive moved to trash.", CottonFolderTrashStatusText.CreateMovedStatus("Archive"));
            Assert.Equal("Move to trash cancelled.", CottonFolderTrashStatusText.CancelledStatus);
            Assert.Equal("Could not move folder to trash.", CottonFolderTrashStatusText.FailedStatus);
            Assert.Equal(
                "Move to trash is taking longer than expected. Refresh and try again.",
                CottonFolderTrashStatusText.TimedOutStatus);
            Assert.Equal(
                "Offline. Move to trash needs internet.",
                CottonFolderTrashStatusText.OfflineUnavailableStatus);
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

        private static CottonFileBrowserEntry CreateFile()
        {
            return CottonFileBrowserEntry.CreateCached(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
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
                eTag: "\"etag-1\"");
        }

        private class FakeFolderTrashClient : ICottonFolderTrashClient
        {
            public Uri? InstanceUri { get; private set; }

            public Guid? FolderId { get; private set; }

            public Task MoveFolderToTrashAsync(
                Uri instanceUri,
                Guid folderId,
                CancellationToken cancellationToken = default)
            {
                InstanceUri = instanceUri;
                FolderId = folderId;
                return Task.CompletedTask;
            }
        }
    }
}
