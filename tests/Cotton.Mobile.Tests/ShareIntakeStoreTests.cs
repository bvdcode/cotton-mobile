using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ShareIntakeStoreTests : IDisposable
    {
        private static readonly Guid IntakeId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid ItemId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        private static readonly DateTime ReceivedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _directory;
        private readonly FileSystemCottonShareIntakeStore _store;

        public ShareIntakeStoreTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-share-intake-tests", Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonShareIntakeStore(new FixedShareIntakePathProvider(_directory));
        }

        [Fact]
        public async Task Add_and_load_roundtrips_pending_single_uri_share()
        {
            var item = new CottonShareIntakeItemSnapshot(
                ItemId,
                CottonShareIntakeItemType.Uri,
                "content://media/external/images/media/42",
                "photo.jpg",
                "image/jpeg");
            CottonShareIntakeSnapshot snapshot = CottonShareIntakeSnapshot.CreatePending(
                IntakeId,
                CottonShareIntakeKind.Send,
                "image/jpeg",
                [item],
                ReceivedAt);

            await _store.AddAsync(snapshot);

            CottonShareIntakeSnapshot loaded = Assert.Single(await _store.LoadAsync());
            Assert.Equal(IntakeId, loaded.Id);
            Assert.Equal(CottonShareIntakeKind.Send, loaded.Kind);
            Assert.Equal(CottonShareIntakeStatus.Pending, loaded.Status);
            Assert.Equal("image/jpeg", loaded.SourceMimeType);
            Assert.True(loaded.CanStageForCaptureInbox);
            CottonShareIntakeItemSnapshot loadedItem = Assert.Single(loaded.Items);
            Assert.Equal(CottonShareIntakeItemType.Uri, loadedItem.Type);
            Assert.Equal("content://media/external/images/media/42", loadedItem.Value);
            Assert.Equal("photo.jpg", loadedItem.DisplayName);
            Assert.Equal("image/jpeg", loadedItem.MimeType);
        }

        [Fact]
        public async Task Save_orders_loaded_snapshots_by_received_time()
        {
            CottonShareIntakeSnapshot later = CreateTextShare(
                Guid.Parse("cccccccc-dddd-eeee-ffff-000000000000"),
                "later",
                ReceivedAt.AddMinutes(2));
            CottonShareIntakeSnapshot earlier = CreateTextShare(
                Guid.Parse("dddddddd-eeee-ffff-0000-111111111111"),
                "earlier",
                ReceivedAt.AddMinutes(-2));

            await _store.SaveAsync([later, earlier]);

            IReadOnlyList<CottonShareIntakeSnapshot> loaded = await _store.LoadAsync();

            Assert.Equal([earlier.Id, later.Id], loaded.Select(item => item.Id).ToArray());
        }

        [Fact]
        public async Task Add_replaces_existing_snapshot_with_same_id()
        {
            await _store.AddAsync(CreateTextShare(IntakeId, "old", ReceivedAt));
            await _store.AddAsync(CreateTextShare(IntakeId, "new", ReceivedAt.AddMinutes(1)));

            CottonShareIntakeSnapshot loaded = Assert.Single(await _store.LoadAsync());

            Assert.Equal("new", Assert.Single(loaded.Items).Value);
        }

        [Fact]
        public async Task Load_deletes_corrupt_metadata_file_and_returns_empty_list()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(CreateMetadataPath(), "{ not valid json");

            IReadOnlyList<CottonShareIntakeSnapshot> loaded = await _store.LoadAsync();

            Assert.Empty(loaded);
            Assert.False(File.Exists(CreateMetadataPath()));
        }

        [Fact]
        public async Task Load_filters_invalid_records_without_discarding_valid_snapshots()
        {
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(
                CreateMetadataPath(),
                """
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-19T12:00:00Z",
                  "items": [
                    {
                      "id": "00000000-0000-0000-0000-000000000000",
                      "kind": 0,
                      "status": 0,
                      "receivedAtUtc": "2026-06-19T12:00:00Z",
                      "items": [
                        {
                          "id": "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
                          "type": 0,
                          "value": "content://bad"
                        }
                      ]
                    },
                    {
                      "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                      "kind": 0,
                      "status": 0,
                      "sourceMimeType": "application/pdf",
                      "receivedAtUtc": "2026-06-19T12:00:00Z",
                      "items": [
                        {
                          "id": "bbbbbbbb-cccc-dddd-eeee-ffffffffffff",
                          "type": 0,
                          "value": "content://docs/report",
                          "displayName": "report.pdf",
                          "mimeType": "application/pdf"
                        }
                      ]
                    }
                  ]
                }
                """);

            CottonShareIntakeSnapshot loaded = Assert.Single(await _store.LoadAsync());

            Assert.Equal(IntakeId, loaded.Id);
            Assert.Equal("report.pdf", Assert.Single(loaded.Items).DisplayName);
        }

        [Fact]
        public async Task Clear_removes_saved_inbox_metadata()
        {
            await _store.AddAsync(CreateTextShare(IntakeId, "hello", ReceivedAt));

            await _store.ClearAsync();

            Assert.False(File.Exists(CreateMetadataPath()));
            Assert.Empty(await _store.LoadAsync());
        }

        [Fact]
        public void Problem_snapshot_preserves_missing_permission_state()
        {
            var item = new CottonShareIntakeItemSnapshot(
                ItemId,
                CottonShareIntakeItemType.Uri,
                "content://media/revoked",
                "revoked.jpg",
                "image/jpeg");

            CottonShareIntakeSnapshot snapshot = CottonShareIntakeSnapshot.CreateProblem(
                IntakeId,
                CottonShareIntakeKind.Send,
                CottonShareIntakeStatus.MissingPermission,
                "image/jpeg",
                [item],
                "Android revoked access to the shared content.",
                ReceivedAt);

            Assert.False(snapshot.CanStageForCaptureInbox);
            Assert.Equal(CottonShareIntakeStatus.MissingPermission, snapshot.Status);
            Assert.Equal("Android revoked access to the shared content.", snapshot.FailureMessage);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonShareIntakeSnapshot CreateTextShare(Guid id, string value, DateTime receivedAtUtc)
        {
            var item = new CottonShareIntakeItemSnapshot(
                Guid.NewGuid(),
                CottonShareIntakeItemType.Text,
                value,
                displayName: null,
                mimeType: "text/plain");
            return CottonShareIntakeSnapshot.CreatePending(
                id,
                CottonShareIntakeKind.Send,
                "text/plain",
                [item],
                receivedAtUtc);
        }

        private string CreateMetadataPath()
        {
            return Path.Combine(_directory, "inbox.json");
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
