using System.Text;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ShareContentStagingStoreTests : IDisposable
    {
        private static readonly Guid IntakeId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid ItemId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        private static readonly DateTime ReceivedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonShareContentStagingStore _store;

        public ShareContentStagingStoreTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-share-content-tests", Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonShareContentStagingStore(new FixedShareIntakePathProvider(_directory));
        }

        [Fact]
        public async Task Stage_copies_content_to_app_private_file_with_safe_name()
        {
            using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello from share"));

            CottonShareStagedContentSnapshot staged =
                await _store.StageAsync(IntakeId, ItemId, "../photo?.jpg", content);

            Assert.Equal(IntakeId, staged.IntakeId);
            Assert.Equal(ItemId, staged.ItemId);
            Assert.Equal("photo?.jpg", staged.FileName);
            Assert.Equal("hello from share", await File.ReadAllTextAsync(staged.Path));
            Assert.Equal(16, staged.SizeBytes);
            Assert.StartsWith(
                Path.Combine(_directory, "Staged", IntakeId.ToString("N"), ItemId.ToString("N")),
                staged.Path,
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task Stage_replaces_existing_item_content()
        {
            await _store.StageAsync(IntakeId, ItemId, "old.txt", CreateStream("old"));

            CottonShareStagedContentSnapshot staged =
                await _store.StageAsync(IntakeId, ItemId, "new.txt", CreateStream("new"));

            Assert.Equal("new.txt", staged.FileName);
            Assert.Equal("new", await File.ReadAllTextAsync(staged.Path));
            string itemDirectory = Path.GetDirectoryName(staged.Path)!;
            Assert.Equal([staged.Path], Directory.EnumerateFiles(itemDirectory).ToArray());
        }

        [Fact]
        public async Task List_returns_staged_files_for_valid_intake_and_item_directories()
        {
            CottonShareStagedContentSnapshot staged =
                await _store.StageAsync(IntakeId, ItemId, "photo.jpg", CreateStream("image"));

            IReadOnlyList<CottonShareStagedContentSnapshot> files = await _store.ListAsync();

            CottonShareStagedContentSnapshot listed = Assert.Single(files);
            Assert.Equal(staged.IntakeId, listed.IntakeId);
            Assert.Equal(staged.ItemId, listed.ItemId);
            Assert.Equal(staged.FileName, listed.FileName);
            Assert.Equal(staged.Path, listed.Path);
            Assert.Equal(staged.SizeBytes, listed.SizeBytes);
        }

        [Fact]
        public async Task Cleanup_removes_staged_content_not_referenced_by_inbox_metadata()
        {
            CottonShareStagedContentSnapshot keep =
                await _store.StageAsync(IntakeId, ItemId, "keep.jpg", CreateStream("keep"));
            CottonShareStagedContentSnapshot remove =
                await _store.StageAsync(
                    Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000"),
                    Guid.Parse("dddddddd-eeee-ffff-0000-111111111111"),
                    "remove.jpg",
                    CreateStream("remove"));
            var item = new CottonShareIntakeItemSnapshot(
                ItemId,
                CottonShareIntakeItemType.Uri,
                "content://media/keep",
                "keep.jpg",
                "image/jpeg")
                .WithStagedContent(keep);
            var inbox = CottonShareIntakeSnapshot.CreatePending(
                IntakeId,
                CottonShareIntakeKind.Send,
                "image/jpeg",
                [item],
                ReceivedAt);

            await _store.CleanupAsync([inbox]);

            Assert.True(File.Exists(keep.Path));
            Assert.False(File.Exists(remove.Path));
        }

        [Fact]
        public async Task DeleteIntake_removes_all_staged_items_for_intake()
        {
            CottonShareStagedContentSnapshot first =
                await _store.StageAsync(IntakeId, ItemId, "first.txt", CreateStream("first"));
            CottonShareStagedContentSnapshot second =
                await _store.StageAsync(
                    IntakeId,
                    Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000"),
                    "second.txt",
                    CreateStream("second"));

            await _store.DeleteIntakeAsync(IntakeId);

            Assert.False(File.Exists(first.Path));
            Assert.False(File.Exists(second.Path));
            Assert.Empty(await _store.ListAsync());
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static MemoryStream CreateStream(string value)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(value));
        }

        private class FixedShareIntakePathProvider : ICottonShareIntakePathProvider
        {
            private readonly string _directory;

            public FixedShareIntakePathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateShareIntakeDirectory()
            {
                return _directory;
            }
        }
    }
}
