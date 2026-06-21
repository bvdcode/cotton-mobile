using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileTrashServiceTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid FileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly DateTime UpdatedAt = new(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Move_file_to_trash_uses_expected_etag_and_server_trash()
        {
            var client = new FakeFileTrashClient();
            var service = new CottonFileTrashService(client);
            CottonFileBrowserEntry file = CreateFile("\"etag-1\"");

            CottonFileTrashMoveResult result = await service.MoveFileToTrashAsync(InstanceUri, file);

            Assert.Equal(FileId, result.FileId);
            Assert.Equal("notes.txt", result.FileName);
            Assert.Equal("notes.txt moved to trash.", result.StatusText);
            Assert.Equal(InstanceUri, client.InstanceUri);
            Assert.Equal(FileId, client.FileId);
            Assert.Equal("\"etag-1\"", client.ExpectedETag);
        }

        [Fact]
        public async Task Move_file_to_trash_requires_fresh_file_etag()
        {
            var client = new FakeFileTrashClient();
            var service = new CottonFileTrashService(client);
            CottonFileBrowserEntry file = CreateFile(eTag: null);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.MoveFileToTrashAsync(InstanceUri, file));

            Assert.Equal(CottonFileTrashStatusText.NeedsRefreshStatus, exception.Message);
            Assert.Null(client.FileId);
        }

        [Fact]
        public async Task Move_file_to_trash_rejects_folders()
        {
            var client = new FakeFileTrashClient();
            var service = new CottonFileTrashService(client);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.MoveFileToTrashAsync(InstanceUri, CreateFolder()));
            Assert.Null(client.FileId);
        }

        [Fact]
        public void File_trash_status_text_is_explicit()
        {
            Assert.Equal(
                "notes.txt will be removed from this folder and can be restored from trash.",
                CottonFileTrashStatusText.CreateConfirmMessage(" notes.txt "));
            Assert.Equal("Moving notes.txt to trash...", CottonFileTrashStatusText.CreateMovingStatus("notes.txt"));
            Assert.Equal("notes.txt moved to trash.", CottonFileTrashStatusText.CreateMovedStatus("notes.txt"));
            Assert.Equal("Move to trash cancelled.", CottonFileTrashStatusText.CancelledStatus);
            Assert.Equal("Could not move file to trash.", CottonFileTrashStatusText.FailedStatus);
            Assert.Equal(
                "Offline. Move to trash needs internet.",
                CottonFileTrashStatusText.OfflineUnavailableStatus);
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
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
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

        private class FakeFileTrashClient : ICottonFileTrashClient
        {
            public Uri? InstanceUri { get; private set; }

            public Guid? FileId { get; private set; }

            public string? ExpectedETag { get; private set; }

            public Task MoveFileToTrashAsync(
                Uri instanceUri,
                Guid fileId,
                string expectedETag,
                CancellationToken cancellationToken = default)
            {
                InstanceUri = instanceUri;
                FileId = fileId;
                ExpectedETag = expectedETag;
                return Task.CompletedTask;
            }
        }
    }
}
