using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class OfflineFilePinStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Uri OtherInstanceUri = new("https://files.cottoncloud.dev");
        private static readonly Guid FileId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly DateTime PinnedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime RemoteUpdatedAt = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);

        private readonly string _rootDirectory;
        private readonly FileSystemCottonOfflineFilePinStore _store;

        public OfflineFilePinStoreTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "cotton-offline-file-pin-tests",
                Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonOfflineFilePinStore(
                new FixedOfflineFileMetadataPathProvider(_rootDirectory));
        }

        [Fact]
        public async Task Save_and_load_roundtrips_offline_file_pin()
        {
            CottonOfflineFilePinSnapshot pin = CreatePin(FileId, "report.pdf");

            await _store.SaveAsync(InstanceUri, [pin]);

            CottonOfflineFilePinSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(FileId, loaded.FileId);
            Assert.Equal("report.pdf", loaded.FileName);
            Assert.Equal(PinnedAt, loaded.PinnedAtUtc);
            Assert.Equal(RemoteUpdatedAt, loaded.RemoteUpdatedAtUtc);
            Assert.Equal(2048, loaded.SizeBytes);
            Assert.Equal("application/pdf", loaded.ContentType);
        }

        [Fact]
        public async Task Add_or_replace_updates_existing_file_pin()
        {
            CottonOfflineFilePinSnapshot first = CreatePin(FileId, "report.pdf");
            CottonOfflineFilePinSnapshot replacement = new(
                FileId,
                "report-renamed.pdf",
                PinnedAt.AddMinutes(1),
                RemoteUpdatedAt.AddMinutes(2),
                4096,
                "application/pdf");

            await _store.AddOrReplaceAsync(InstanceUri, first);
            await _store.AddOrReplaceAsync(InstanceUri, replacement);

            CottonOfflineFilePinSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal("report-renamed.pdf", loaded.FileName);
            Assert.Equal(PinnedAt.AddMinutes(1), loaded.PinnedAtUtc);
            Assert.Equal(4096, loaded.SizeBytes);
        }

        [Fact]
        public async Task Save_filters_duplicate_file_ids_by_last_entry()
        {
            CottonOfflineFilePinSnapshot first = CreatePin(FileId, "report.pdf");
            CottonOfflineFilePinSnapshot replacement = CreatePin(FileId, "report-new.pdf");

            await _store.SaveAsync(InstanceUri, [first, replacement]);

            CottonOfflineFilePinSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal("report-new.pdf", loaded.FileName);
        }

        [Fact]
        public async Task Load_returns_empty_when_metadata_is_missing()
        {
            IReadOnlyList<CottonOfflineFilePinSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
        }

        [Fact]
        public async Task Load_deletes_corrupt_metadata_and_returns_empty()
        {
            string metadataPath = CreateMetadataPath(InstanceUri);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(metadataPath, "{ not valid json");

            IReadOnlyList<CottonOfflineFilePinSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
            Assert.False(File.Exists(metadataPath));
        }

        [Fact]
        public async Task Load_filters_invalid_records_without_discarding_valid_pins()
        {
            string metadataPath = CreateMetadataPath(InstanceUri);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(
                metadataPath,
                """
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-19T12:00:00Z",
                  "items": [
                    {
                      "fileId": "00000000-0000-0000-0000-000000000000",
                      "fileName": "bad.pdf",
                      "pinnedAtUtc": "2026-06-19T12:00:00Z",
                      "remoteUpdatedAtUtc": "2026-06-19T10:00:00Z"
                    },
                    {
                      "fileId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                      "fileName": "good.pdf",
                      "pinnedAtUtc": "2026-06-19T12:00:00Z",
                      "remoteUpdatedAtUtc": "2026-06-19T10:00:00Z",
                      "sizeBytes": 2048,
                      "contentType": "application/pdf"
                    }
                  ]
                }
                """);

            CottonOfflineFilePinSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));

            Assert.Equal(FileId, loaded.FileId);
            Assert.Equal("good.pdf", loaded.FileName);
        }

        [Fact]
        public async Task Remove_deletes_single_file_pin()
        {
            Guid otherFileId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
            await _store.SaveAsync(InstanceUri, [
                CreatePin(FileId, "report.pdf"),
                CreatePin(otherFileId, "photo.jpg"),
            ]);

            bool removed = await _store.RemoveAsync(InstanceUri, FileId);

            Assert.True(removed);
            CottonOfflineFilePinSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(otherFileId, loaded.FileId);
        }

        [Fact]
        public async Task Remove_returns_false_when_file_pin_is_missing()
        {
            await _store.SaveAsync(InstanceUri, [CreatePin(FileId, "report.pdf")]);

            bool removed = await _store.RemoveAsync(
                InstanceUri,
                Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"));

            Assert.False(removed);
            Assert.Single(await _store.LoadAsync(InstanceUri));
        }

        [Fact]
        public async Task Store_isolates_offline_file_pins_by_instance()
        {
            await _store.SaveAsync(InstanceUri, [CreatePin(FileId, "report.pdf")]);
            await _store.SaveAsync(OtherInstanceUri, [
                CreatePin(
                    Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                    "other.pdf"),
            ]);

            CottonOfflineFilePinSnapshot first = Assert.Single(await _store.LoadAsync(InstanceUri));
            CottonOfflineFilePinSnapshot second = Assert.Single(await _store.LoadAsync(OtherInstanceUri));

            Assert.Equal("report.pdf", first.FileName);
            Assert.Equal("other.pdf", second.FileName);
            Assert.NotEqual(
                Path.GetDirectoryName(CreateMetadataPath(InstanceUri)),
                Path.GetDirectoryName(CreateMetadataPath(OtherInstanceUri)));
        }

        [Fact]
        public async Task Clear_removes_offline_file_pin_metadata()
        {
            await _store.SaveAsync(InstanceUri, [CreatePin(FileId, "report.pdf")]);

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
                previewHashEncryptedHex: null);

            CottonOfflineFilePinSnapshot pin = CottonOfflineFilePinSnapshot.Create(file, PinnedAt);

            Assert.Equal(FileId, pin.FileId);
            Assert.Equal("report.pdf", pin.FileName);
            Assert.Equal(PinnedAt, pin.PinnedAtUtc);
            Assert.Equal(RemoteUpdatedAt, pin.RemoteUpdatedAtUtc);
            Assert.Equal(2048, pin.SizeBytes);
            Assert.Equal("application/pdf", pin.ContentType);
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
                previewHashEncryptedHex: null);

            Assert.Throws<ArgumentException>(() => CottonOfflineFilePinSnapshot.Create(folder, PinnedAt));
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }

        private static CottonOfflineFilePinSnapshot CreatePin(Guid fileId, string fileName)
        {
            return new CottonOfflineFilePinSnapshot(
                fileId,
                fileName,
                PinnedAt,
                RemoteUpdatedAt,
                2048,
                "application/pdf");
        }

        private string CreateMetadataPath(Uri instanceUri)
        {
            return Path.Combine(CreateInstanceDirectory(instanceUri), "offline-files.json");
        }

        private string CreateInstanceDirectory(Uri instanceUri)
        {
            return Path.Combine(_rootDirectory, instanceUri.Host);
        }

        private class FixedOfflineFileMetadataPathProvider : ICottonOfflineFileMetadataPathProvider
        {
            private readonly string _rootDirectory;

            public FixedOfflineFileMetadataPathProvider(string rootDirectory)
            {
                _rootDirectory = rootDirectory;
            }

            public string CreateOfflineFileMetadataDirectory(Uri instanceUri)
            {
                return Path.Combine(_rootDirectory, instanceUri.Host);
            }
        }
    }
}
