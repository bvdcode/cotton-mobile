using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RecentFileStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Uri OtherInstanceUri = new("https://files.cottoncloud.dev");
        private static readonly Guid FileId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OtherFileId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
        private static readonly DateTime RemoteUpdatedAt = new(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime UsedAt = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _rootDirectory;
        private readonly FileSystemCottonRecentFileStore _store;

        public RecentFileStoreTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "cotton-recent-file-tests",
                Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonRecentFileStore(
                new FixedRecentFileMetadataPathProvider(_rootDirectory),
                new CottonRecentFileStoreOptions(3));
        }

        [Fact]
        public async Task Save_and_load_roundtrips_recent_file()
        {
            CottonRecentFileSnapshot recent = CreateRecent(FileId, "report.pdf", UsedAt);

            await _store.SaveAsync(InstanceUri, [recent]);

            CottonRecentFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(FileId, loaded.FileId);
            Assert.Equal("report.pdf", loaded.FileName);
            Assert.Equal("PDF", loaded.Kind);
            Assert.Equal("PDF", loaded.BadgeText);
            Assert.Equal(RemoteUpdatedAt, loaded.RemoteUpdatedAtUtc);
            Assert.Equal(2048, loaded.SizeBytes);
            Assert.Equal("application/pdf", loaded.ContentType);
            Assert.Equal(UsedAt, loaded.LastUsedAtUtc);
            Assert.Equal(CottonRecentFileActionKind.Opened, loaded.LastAction);
        }

        [Fact]
        public async Task Record_replaces_existing_file_and_orders_newest_first()
        {
            await _store.RecordAsync(InstanceUri, CreateRecent(FileId, "report.pdf", UsedAt));
            await _store.RecordAsync(InstanceUri, CreateRecent(OtherFileId, "photo.jpg", UsedAt.AddMinutes(1)));
            await _store.RecordAsync(
                InstanceUri,
                new CottonRecentFileSnapshot(
                    FileId,
                    "report-renamed.pdf",
                    "PDF",
                    "PDF",
                    RemoteUpdatedAt.AddMinutes(2),
                    4096,
                    "application/pdf",
                    UsedAt.AddMinutes(2),
                    CottonRecentFileActionKind.Shared));

            IReadOnlyList<CottonRecentFileSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Collection(
                loaded,
                first =>
                {
                    Assert.Equal(FileId, first.FileId);
                    Assert.Equal("report-renamed.pdf", first.FileName);
                    Assert.Equal(CottonRecentFileActionKind.Shared, first.LastAction);
                },
                second => Assert.Equal(OtherFileId, second.FileId));
        }

        [Fact]
        public async Task Save_limits_recent_files_to_configured_count()
        {
            await _store.SaveAsync(InstanceUri, [
                CreateRecent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "one.txt", UsedAt),
                CreateRecent(Guid.Parse("00000000-0000-0000-0000-000000000002"), "two.txt", UsedAt.AddMinutes(1)),
                CreateRecent(Guid.Parse("00000000-0000-0000-0000-000000000003"), "three.txt", UsedAt.AddMinutes(2)),
                CreateRecent(Guid.Parse("00000000-0000-0000-0000-000000000004"), "four.txt", UsedAt.AddMinutes(3)),
            ]);

            IReadOnlyList<CottonRecentFileSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Equal(3, loaded.Count);
            Assert.Equal("four.txt", loaded[0].FileName);
            Assert.Equal("three.txt", loaded[1].FileName);
            Assert.Equal("two.txt", loaded[2].FileName);
        }

        [Fact]
        public async Task Load_returns_empty_when_metadata_is_missing()
        {
            IReadOnlyList<CottonRecentFileSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
        }

        [Fact]
        public async Task Load_deletes_corrupt_metadata_and_returns_empty()
        {
            string metadataPath = CreateMetadataPath(InstanceUri);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(metadataPath, "{ not valid json");

            IReadOnlyList<CottonRecentFileSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
            Assert.False(File.Exists(metadataPath));
        }

        [Fact]
        public async Task Load_filters_invalid_records_without_discarding_valid_recent_files()
        {
            string metadataPath = CreateMetadataPath(InstanceUri);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(
                metadataPath,
                """
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-21T12:00:00Z",
                  "items": [
                    {
                      "fileId": "00000000-0000-0000-0000-000000000000",
                      "fileName": "bad.pdf",
                      "kind": "PDF",
                      "badgeText": "PDF",
                      "remoteUpdatedAtUtc": "2026-06-21T10:00:00Z",
                      "sizeBytes": 2048,
                      "contentType": "application/pdf",
                      "lastUsedAtUtc": "2026-06-21T12:00:00Z",
                      "lastAction": 0
                    },
                    {
                      "fileId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                      "fileName": "good.pdf",
                      "kind": "PDF",
                      "badgeText": "PDF",
                      "remoteUpdatedAtUtc": "2026-06-21T10:00:00Z",
                      "sizeBytes": 2048,
                      "contentType": "application/pdf",
                      "lastUsedAtUtc": "2026-06-21T12:00:00Z",
                      "lastAction": 0
                    }
                  ]
                }
                """);

            CottonRecentFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));

            Assert.Equal(FileId, loaded.FileId);
            Assert.Equal("good.pdf", loaded.FileName);
        }

        [Fact]
        public async Task Remove_deletes_single_recent_file()
        {
            await _store.SaveAsync(InstanceUri, [
                CreateRecent(FileId, "report.pdf", UsedAt),
                CreateRecent(OtherFileId, "photo.jpg", UsedAt.AddMinutes(1)),
            ]);

            bool removed = await _store.RemoveAsync(InstanceUri, FileId);

            Assert.True(removed);
            CottonRecentFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(OtherFileId, loaded.FileId);
        }

        [Fact]
        public async Task Store_isolates_recent_files_by_instance()
        {
            await _store.SaveAsync(InstanceUri, [CreateRecent(FileId, "report.pdf", UsedAt)]);
            await _store.SaveAsync(OtherInstanceUri, [CreateRecent(OtherFileId, "other.pdf", UsedAt)]);

            CottonRecentFileSnapshot first = Assert.Single(await _store.LoadAsync(InstanceUri));
            CottonRecentFileSnapshot second = Assert.Single(await _store.LoadAsync(OtherInstanceUri));

            Assert.Equal("report.pdf", first.FileName);
            Assert.Equal("other.pdf", second.FileName);
            Assert.NotEqual(
                Path.GetDirectoryName(CreateMetadataPath(InstanceUri)),
                Path.GetDirectoryName(CreateMetadataPath(OtherInstanceUri)));
        }

        [Fact]
        public async Task Clear_removes_recent_file_metadata()
        {
            await _store.SaveAsync(InstanceUri, [CreateRecent(FileId, "report.pdf", UsedAt)]);

            await _store.ClearAsync(InstanceUri);

            Assert.False(File.Exists(CreateMetadataPath(InstanceUri)));
            Assert.Empty(await _store.LoadAsync(InstanceUri));
        }

        [Fact]
        public void Snapshot_create_captures_file_browser_metadata()
        {
            CottonFileBrowserEntry file = CottonFileBrowserEntry.CreateCached(
                FileId,
                CottonFileBrowserEntryType.File,
                " report.pdf ",
                "PDF",
                "2 KB · PDF",
                "More",
                "PDF",
                RemoteUpdatedAt,
                2048,
                " application/pdf ",
                previewHashEncryptedHex: null,
                eTag: null);

            CottonRecentFileSnapshot recent = CottonRecentFileSnapshot.Create(
                file,
                CottonRecentFileActionKind.Downloaded,
                UsedAt);

            Assert.Equal(FileId, recent.FileId);
            Assert.Equal("report.pdf", recent.FileName);
            Assert.Equal("PDF", recent.Kind);
            Assert.Equal("PDF", recent.BadgeText);
            Assert.Equal(RemoteUpdatedAt, recent.RemoteUpdatedAtUtc);
            Assert.Equal(2048, recent.SizeBytes);
            Assert.Equal("application/pdf", recent.ContentType);
            Assert.Equal(UsedAt, recent.LastUsedAtUtc);
            Assert.Equal(CottonRecentFileActionKind.Downloaded, recent.LastAction);
        }

        [Fact]
        public void Snapshot_create_rejects_folder_entries()
        {
            CottonFileBrowserEntry folder = CottonFileBrowserEntry.CreateCached(
                FileId,
                CottonFileBrowserEntryType.Folder,
                "Reports",
                "Folder",
                "Folder",
                "Open",
                "Folder",
                RemoteUpdatedAt,
                sizeBytes: null,
                contentType: null,
                previewHashEncryptedHex: null,
                eTag: null);

            Assert.Throws<ArgumentException>(() =>
                CottonRecentFileSnapshot.Create(folder, CottonRecentFileActionKind.Opened, UsedAt));
        }

        [Fact]
        public void List_snapshot_formats_empty_and_recent_items()
        {
            CottonRecentFileListSnapshot empty = CottonRecentFileListSnapshot.Create([]);

            Assert.True(empty.IsEmpty);
            Assert.False(empty.IsListVisible);
            Assert.Equal("No recent files", empty.SummaryText);
            Assert.Equal("No recent files yet", empty.EmptyMessage);

            CottonRecentFileListSnapshot list = CottonRecentFileListSnapshot.Create([
                CreateRecent(OtherFileId, "photo.jpg", UsedAt.AddMinutes(1), CottonRecentFileActionKind.Shared),
                CreateRecent(FileId, "report.pdf", UsedAt, CottonRecentFileActionKind.Opened),
            ]);

            Assert.False(list.IsEmpty);
            Assert.True(list.IsListVisible);
            Assert.Equal("2 recent files", list.SummaryText);
            Assert.Collection(
                list.Items,
                first =>
                {
                    Assert.Equal("photo.jpg", first.FileName);
                    Assert.Equal("PDF", first.Kind);
                    Assert.Equal(RemoteUpdatedAt, first.RemoteUpdatedAtUtc);
                    Assert.Equal(2048, first.SizeBytes);
                    Assert.Equal("application/pdf", first.ContentType);
                    Assert.Equal("2 KB · PDF · Shared 2026-06-21 12:01 UTC", first.DetailText);
                    Assert.Equal("Shared", first.LastActionText);
                },
                second =>
                {
                    Assert.Equal("report.pdf", second.FileName);
                    Assert.Equal("2 KB · PDF · Opened 2026-06-21 12:00 UTC", second.DetailText);
                    Assert.Equal("Opened", second.LastActionText);
                });
        }

        [Fact]
        public void List_snapshot_orders_matching_timestamps_by_file_name()
        {
            CottonRecentFileListSnapshot list = CottonRecentFileListSnapshot.Create([
                CreateRecent(OtherFileId, "zebra.pdf", UsedAt),
                CreateRecent(FileId, "alpha.pdf", UsedAt),
            ]);

            Assert.Collection(
                list.Items,
                first => Assert.Equal("alpha.pdf", first.FileName),
                second => Assert.Equal("zebra.pdf", second.FileName));
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }

        private static CottonRecentFileSnapshot CreateRecent(
            Guid fileId,
            string fileName,
            DateTime usedAt,
            CottonRecentFileActionKind action = CottonRecentFileActionKind.Opened)
        {
            return new CottonRecentFileSnapshot(
                fileId,
                fileName,
                "PDF",
                "PDF",
                RemoteUpdatedAt,
                2048,
                "application/pdf",
                usedAt,
                action);
        }

        private string CreateMetadataPath(Uri instanceUri)
        {
            return Path.Combine(CreateInstanceDirectory(instanceUri), "recent-files.json");
        }

        private string CreateInstanceDirectory(Uri instanceUri)
        {
            return Path.Combine(_rootDirectory, instanceUri.Host);
        }

        private class FixedRecentFileMetadataPathProvider : ICottonRecentFileMetadataPathProvider
        {
            private readonly string _rootDirectory;

            public FixedRecentFileMetadataPathProvider(string rootDirectory)
            {
                _rootDirectory = rootDirectory;
            }

            public string CreateRecentFileMetadataDirectory(Uri instanceUri)
            {
                return Path.Combine(_rootDirectory, instanceUri.Host);
            }
        }
    }
}
