using Cotton.Files;
using Cotton.Mobile.Services;
using Cotton.Nodes;
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

        [Fact]
        public void Trash_display_state_filters_by_search_text()
        {
            CottonTrashListDisplayState state = CottonTrashListDisplayState.Create(
                [
                    CreateFile("Budget.txt", UpdatedAt, sizeBytes: 128, contentType: "text/plain"),
                    CreateFile("Logo.png", UpdatedAt, sizeBytes: 256, contentType: "image/png"),
                    CreateFolder("Archive", UpdatedAt),
                ],
                " image ",
                isSearchOpen: true,
                CottonFileBrowserSortMode.Name,
                CottonFileBrowserViewMode.List);

            CottonFileBrowserEntry item = Assert.Single(state.Items);
            Assert.Equal("Logo.png", item.Name);
            Assert.Equal("image", state.SearchText);
            Assert.True(state.IsSearchActive);
            Assert.True(state.IsSearchVisible);
            Assert.False(state.IsSortButtonVisible);
            Assert.False(state.IsViewButtonVisible);
            Assert.Equal("1 match · A-Z", state.SummaryText);
            Assert.Equal("×", state.SearchButtonText);
            Assert.Equal("Clear trash search", state.SearchButtonDescription);
        }

        [Fact]
        public void Trash_display_state_keeps_no_match_empty_state_distinct_from_empty_trash()
        {
            CottonTrashListDisplayState state = CottonTrashListDisplayState.Create(
                [CreateFile("Budget.txt", UpdatedAt)],
                "missing",
                isSearchOpen: true,
                CottonFileBrowserSortMode.Updated,
                CottonFileBrowserViewMode.List);

            Assert.Empty(state.Items);
            Assert.True(state.IsEmpty);
            Assert.False(state.IsListVisible);
            Assert.Equal("No trash matches", state.EmptyMessage);
            Assert.Equal("Try another search.", state.EmptyDetails);
            Assert.Equal("0 matches · Newest", state.SummaryText);
        }

        [Fact]
        public void Trash_display_state_sorts_folders_before_files()
        {
            IReadOnlyList<CottonFileBrowserEntry> items =
            [
                CreateFile("zeta.txt", UpdatedAt, sizeBytes: 512),
                CreateFolder("Archive", UpdatedAt.AddDays(-1)),
                CreateFile("alpha.txt", UpdatedAt, sizeBytes: 1024),
            ];

            CottonTrashListDisplayState byName = CottonTrashListDisplayState.Create(
                items,
                string.Empty,
                isSearchOpen: false,
                CottonFileBrowserSortMode.Name,
                CottonFileBrowserViewMode.List);
            CottonTrashListDisplayState bySize = CottonTrashListDisplayState.Create(
                items,
                string.Empty,
                isSearchOpen: false,
                CottonFileBrowserSortMode.Size,
                CottonFileBrowserViewMode.List);

            Assert.Equal(["Archive", "alpha.txt", "zeta.txt"], byName.Items.Select(item => item.Name).ToArray());
            Assert.Equal(["Archive", "alpha.txt", "zeta.txt"], bySize.Items.Select(item => item.Name).ToArray());
            Assert.Equal("3 items in trash · A-Z", byName.SummaryText);
            Assert.Equal("3 items in trash · Size", bySize.SummaryText);
        }

        [Fact]
        public void Trash_display_state_sorts_by_type_and_updated_time()
        {
            IReadOnlyList<CottonFileBrowserEntry> items =
            [
                CreateFile("latest.txt", UpdatedAt.AddMinutes(3), contentType: "text/plain"),
                CreateFile("photo.png", UpdatedAt.AddMinutes(1), contentType: "image/png"),
                CreateFile("notes.md", UpdatedAt.AddMinutes(2), contentType: "text/markdown"),
            ];

            CottonTrashListDisplayState byType = CottonTrashListDisplayState.Create(
                items,
                string.Empty,
                isSearchOpen: false,
                CottonFileBrowserSortMode.Type,
                CottonFileBrowserViewMode.List);
            CottonTrashListDisplayState byUpdated = CottonTrashListDisplayState.Create(
                items,
                string.Empty,
                isSearchOpen: false,
                CottonFileBrowserSortMode.Updated,
                CottonFileBrowserViewMode.List);

            Assert.Equal(["photo.png", "latest.txt", "notes.md"], byType.Items.Select(item => item.Name).ToArray());
            Assert.Equal(["latest.txt", "notes.md", "photo.png"], byUpdated.Items.Select(item => item.Name).ToArray());
            Assert.Equal("New", byUpdated.SortButtonText);
        }

        [Fact]
        public void Trash_display_state_exposes_list_and_tile_modes()
        {
            CottonTrashListDisplayState list = CottonTrashListDisplayState.Create(
                [CreateFile("Budget.txt", UpdatedAt)],
                string.Empty,
                isSearchOpen: false,
                CottonFileBrowserSortMode.Name,
                CottonFileBrowserViewMode.List);
            CottonTrashListDisplayState tiles = CottonTrashListDisplayState.Create(
                [CreateFile("Budget.txt", UpdatedAt)],
                string.Empty,
                isSearchOpen: false,
                CottonFileBrowserSortMode.Name,
                CottonFileBrowserViewMode.Tiles);

            Assert.True(list.IsListVisible);
            Assert.False(list.IsTileVisible);
            Assert.Equal("☰", list.ViewButtonText);
            Assert.False(tiles.IsListVisible);
            Assert.True(tiles.IsTileVisible);
            Assert.Equal("▦", tiles.ViewButtonText);
        }

        private static CottonFileBrowserEntry CreateFile(string name, DateTime updatedAt)
        {
            return CreateFile(name, updatedAt, sizeBytes: 128);
        }

        private static CottonFileBrowserEntry CreateFile(
            string name,
            DateTime updatedAt,
            long sizeBytes = 128,
            string contentType = "text/plain")
        {
            return CottonFileBrowserEntry.FromFile(
                new NodeFileManifestDto
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    ContentType = contentType,
                    SizeBytes = sizeBytes,
                    CreatedAt = UpdatedAt,
                    UpdatedAt = updatedAt,
                });
        }

        private static CottonFileBrowserEntry CreateFolder(string name, DateTime updatedAt)
        {
            return CottonFileBrowserEntry.FromNode(
                new NodeDto
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreatedAt = UpdatedAt,
                    UpdatedAt = updatedAt,
                });
        }
    }
}
