using Cotton.Files;
using Cotton.Mobile.Services;
using Cotton.Nodes;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashBrowserServiceTests
    {
        private static readonly Uri InstanceUri = new("https://cloud.example");
        private static readonly Guid TrashRootId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        private static readonly DateTime CreatedAt = new(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetRootAsync_loads_all_pages_and_orders_mixed_entries_by_updated_descending()
        {
            var client = new FakeTrashBrowserClient(
                CreateRoot(),
                [
                    new NodeContentDto
                    {
                        TotalCount = 101,
                        Nodes =
                        [
                            CreateNode(
                                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                                "Older folder",
                                CreatedAt.AddMinutes(1)),
                        ],
                        Files =
                        [
                            CreateFile(
                                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                                "Newest.txt",
                                CreatedAt.AddMinutes(3)),
                        ],
                    },
                    new NodeContentDto
                    {
                        TotalCount = 101,
                        Nodes =
                        [
                            CreateNode(
                                Guid.Parse("44444444-4444-4444-8444-444444444444"),
                                "Middle folder",
                                CreatedAt.AddMinutes(2)),
                        ],
                    },
                ]);
            var service = new CottonTrashBrowserService(client);

            CottonFolderContent content = await service.GetRootAsync(InstanceUri);

            Assert.Equal(TrashRootId, content.FolderId);
            Assert.Equal("Trash", content.FolderName);
            Assert.Equal(
                ["Newest.txt", "Middle folder", "Older folder"],
                content.Entries.Select(entry => entry.Name).ToArray());
            Assert.Equal(
                [CottonFileBrowserEntryType.File, CottonFileBrowserEntryType.Folder, CottonFileBrowserEntryType.Folder],
                content.Entries.Select(entry => entry.Type).ToArray());
            Assert.Equal([1, 2], client.RequestedPages);
        }

        [Fact]
        public async Task GetRootAsync_keeps_empty_trash_explicit()
        {
            var client = new FakeTrashBrowserClient(
                CreateRoot(),
                [
                    new NodeContentDto
                    {
                        TotalCount = 0,
                    },
                ]);
            var service = new CottonTrashBrowserService(client);

            CottonFolderContent content = await service.GetRootAsync(InstanceUri);

            Assert.Empty(content.Entries);
            Assert.Equal([1], client.RequestedPages);
        }

        private static NodeDto CreateRoot()
        {
            return CreateNode(TrashRootId, string.Empty, CreatedAt);
        }

        private static NodeDto CreateNode(Guid id, string name, DateTime updatedAt)
        {
            return new NodeDto
            {
                Id = id,
                Name = name,
                CreatedAt = CreatedAt,
                UpdatedAt = updatedAt,
            };
        }

        private static NodeFileManifestDto CreateFile(Guid id, string name, DateTime updatedAt)
        {
            return new NodeFileManifestDto
            {
                Id = id,
                Name = name,
                ContentType = "text/plain",
                SizeBytes = 1024,
                CreatedAt = CreatedAt,
                UpdatedAt = updatedAt,
            };
        }

        private class FakeTrashBrowserClient : ICottonTrashBrowserClient
        {
            private readonly NodeDto _root;
            private readonly IReadOnlyList<NodeContentDto> _pages;

            public FakeTrashBrowserClient(NodeDto root, IReadOnlyList<NodeContentDto> pages)
            {
                _root = root;
                _pages = pages;
            }

            public List<int> RequestedPages { get; } = [];

            public Task<NodeDto> GetTrashRootAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                Assert.Equal(InstanceUri, instanceUri);
                return Task.FromResult(_root);
            }

            public Task<NodeContentDto> GetChildrenAsync(
                Uri instanceUri,
                Guid trashFolderId,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
            {
                Assert.Equal(InstanceUri, instanceUri);
                Assert.Equal(TrashRootId, trashFolderId);
                Assert.Equal(100, pageSize);
                RequestedPages.Add(page);
                return Task.FromResult(_pages[page - 1]);
            }
        }
    }
}
