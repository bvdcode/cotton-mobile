using Cotton.Files;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashBrowserPresentationTests
    {
        private static readonly Guid FolderId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        private static readonly DateTime UpdatedAt = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Trash_list_snapshot_keeps_empty_state_explicit()
        {
            CottonTrashListSnapshot snapshot = CottonTrashListSnapshot.Create(
                new CottonFolderContent(FolderId, "Trash", []));

            Assert.Empty(snapshot.Items);
            Assert.Equal("Trash is empty", snapshot.SummaryText);
            Assert.Equal("Trash is empty", snapshot.EmptyMessage);
            Assert.Equal("Deleted files and folders will appear here.", snapshot.EmptyDetails);
            Assert.True(snapshot.IsEmpty);
            Assert.False(snapshot.IsListVisible);
        }

        [Fact]
        public void Trash_list_snapshot_keeps_service_order_and_summary()
        {
            CottonTrashListSnapshot snapshot = CottonTrashListSnapshot.Create(
                new CottonFolderContent(
                    FolderId,
                    "Trash",
                    [
                        CreateFile("Newest.txt", UpdatedAt.AddMinutes(2)),
                        CreateFile("Older.txt", UpdatedAt),
                    ]));

            Assert.Equal(["Newest.txt", "Older.txt"], snapshot.Items.Select(item => item.Name).ToArray());
            Assert.Equal("2 items in trash", snapshot.SummaryText);
            Assert.False(snapshot.IsEmpty);
            Assert.True(snapshot.IsListVisible);
        }

        [Fact]
        public void Trash_list_status_text_formats_loaded_state()
        {
            Assert.Equal("Trash is empty", CottonTrashListStatusText.CreateLoadedStatus(0));
            Assert.Equal("1 item in trash", CottonTrashListStatusText.CreateLoadedStatus(1));
            Assert.Equal("2 items in trash", CottonTrashListStatusText.CreateLoadedStatus(2));
            Assert.Equal("Loading trash...", CottonTrashListStatusText.LoadingStatus);
            Assert.Equal("Offline. Trash needs internet.", CottonTrashListStatusText.OfflineUnavailableStatus);
        }

        [Fact]
        public void Trash_list_summary_rejects_negative_counts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CottonTrashListSnapshot.CreateSummaryText(-1));
        }

        private static CottonFileBrowserEntry CreateFile(string name, DateTime updatedAt)
        {
            return CottonFileBrowserEntry.FromFile(
                new NodeFileManifestDto
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    ContentType = "text/plain",
                    SizeBytes = 128,
                    CreatedAt = UpdatedAt,
                    UpdatedAt = updatedAt,
                });
        }
    }
}
