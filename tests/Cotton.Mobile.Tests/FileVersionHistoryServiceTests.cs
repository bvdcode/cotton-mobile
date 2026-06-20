using Cotton.Files;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class FileVersionHistoryServiceTests
    {
        private static readonly Uri InstanceUri = new("https://app.example.test");
        private static readonly Guid FileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid VersionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Fact]
        public async Task Service_loads_versions_for_file_entry()
        {
            var client = new FakeFileVersionHistoryClient(
                [
                    new FileVersionDto
                    {
                        Id = VersionId,
                        NodeFileId = FileId,
                        FileManifestId = VersionId,
                        Name = "notes.txt",
                        ContentType = "text/plain",
                        SizeBytes = 42,
                        VersionNumber = 1,
                        IsCurrent = true,
                        IsOriginal = true,
                        CanDelete = false,
                        CreatedAt = new DateTime(2026, 6, 20, 1, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2026, 6, 20, 2, 0, 0, DateTimeKind.Utc),
                    },
                ]);
            var service = new CottonFileVersionHistoryService(client);

            CottonFileVersionListSnapshot snapshot = await service.GetVersionsAsync(
                InstanceUri,
                CreateFile(),
                TimeZoneInfo.Utc);

            Assert.Equal(InstanceUri, client.InstanceUri);
            Assert.Equal(FileId, client.FileId);
            Assert.Equal(CottonFileVersionListStatus.Ready, snapshot.Status);
            Assert.Equal("1 version for notes.txt.", snapshot.SummaryText);
            Assert.Single(snapshot.Items);
        }

        [Fact]
        public async Task Service_rejects_folder_entries()
        {
            var client = new FakeFileVersionHistoryClient([]);
            var service = new CottonFileVersionHistoryService(client);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetVersionsAsync(
                    InstanceUri,
                    CottonFileBrowserEntry.FromNode(
                        new Cotton.Nodes.NodeDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Folder",
                            UpdatedAt = DateTime.UtcNow,
                        }),
                    TimeZoneInfo.Utc));
            Assert.Equal(Guid.Empty, client.FileId);
        }

        private static CottonFileBrowserEntry CreateFile()
        {
            return CottonFileBrowserEntry.CreateFile(
                FileId,
                "notes.txt",
                new DateTime(2026, 6, 20, 2, 0, 0, DateTimeKind.Utc),
                42,
                "text/plain",
                previewHashEncryptedHex: null,
                eTag: "\"etag\"");
        }

        private class FakeFileVersionHistoryClient : ICottonFileVersionHistoryClient
        {
            private readonly IReadOnlyList<FileVersionDto> _versions;

            public FakeFileVersionHistoryClient(IReadOnlyList<FileVersionDto> versions)
            {
                _versions = versions;
            }

            public Uri? InstanceUri { get; private set; }

            public Guid FileId { get; private set; }

            public Task<IReadOnlyList<FileVersionDto>> GetVersionsAsync(
                Uri instanceUri,
                Guid fileId,
                CancellationToken cancellationToken = default)
            {
                InstanceUri = instanceUri;
                FileId = fileId;
                return Task.FromResult(_versions);
            }
        }
    }
}
