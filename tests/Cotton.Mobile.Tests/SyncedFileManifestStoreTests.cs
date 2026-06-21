using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncedFileManifestStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid CloudFolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        private static readonly DateTime RemoteUpdatedAt = new(2026, 6, 20, 14, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SyncedAt = new(2026, 6, 20, 14, 5, 0, DateTimeKind.Utc);

        private readonly string _rootDirectory;
        private readonly CottonSyncRootSnapshot _syncRoot;
        private readonly FileSystemCottonSyncedFileManifestStore _store;

        public SyncedFileManifestStoreTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "cotton-synced-file-manifest-tests",
                Guid.NewGuid().ToString("N"));
            _syncRoot = CreateRoot("app-private-sync-root");
            _store = new FileSystemCottonSyncedFileManifestStore(
                new FixedSyncedFileManifestPathProvider(_rootDirectory));
        }

        [Fact]
        public async Task Save_and_load_roundtrips_synced_file_manifest()
        {
            CottonSyncedFileSnapshot file = CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"");

            await _store.SaveAsync(InstanceUri, _syncRoot, [file]);

            CottonSyncedFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal(FileId, loaded.FileId);
            Assert.Equal("report.pdf", loaded.FileName);
            Assert.Equal("report.pdf", loaded.RelativePath);
            Assert.Equal("\"etag-1\"", loaded.ETag);
            Assert.Equal(RemoteUpdatedAt, loaded.RemoteUpdatedAtUtc);
            Assert.Equal(2048, loaded.SizeBytes);
            Assert.Equal("application/pdf", loaded.ContentType);
            Assert.Equal(SyncedAt, loaded.SyncedAtUtc);
        }

        [Fact]
        public async Task Add_or_replace_updates_existing_manifest_item()
        {
            CottonSyncedFileSnapshot first = CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"");
            CottonSyncedFileSnapshot replacement = new(
                FileId,
                "report-renamed.pdf",
                "\"etag-2\"",
                RemoteUpdatedAt.AddMinutes(2),
                4096,
                "application/pdf",
                SyncedAt.AddMinutes(3));

            await _store.AddOrReplaceAsync(InstanceUri, _syncRoot, first);
            await _store.AddOrReplaceAsync(InstanceUri, _syncRoot, replacement);

            CottonSyncedFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal("report-renamed.pdf", loaded.FileName);
            Assert.Equal("\"etag-2\"", loaded.ETag);
            Assert.Equal(4096, loaded.SizeBytes);
        }

        [Fact]
        public async Task Add_or_replace_updates_existing_manifest_item_at_same_relative_path()
        {
            Guid replacementFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            CottonSyncedFileSnapshot first = CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"");
            CottonSyncedFileSnapshot replacement = CreateSyncedFile(replacementFileId, "report.pdf", "\"etag-2\"");

            await _store.AddOrReplaceAsync(InstanceUri, _syncRoot, first);
            await _store.AddOrReplaceAsync(InstanceUri, _syncRoot, replacement);

            CottonSyncedFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal(replacementFileId, loaded.FileId);
            Assert.Equal("report.pdf", loaded.RelativePath);
            Assert.Equal("\"etag-2\"", loaded.ETag);
        }

        [Fact]
        public async Task Save_filters_duplicate_file_ids_by_last_entry()
        {
            CottonSyncedFileSnapshot first = CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"");
            CottonSyncedFileSnapshot replacement = CreateSyncedFile(FileId, "report-new.pdf", "\"etag-2\"");

            await _store.SaveAsync(InstanceUri, _syncRoot, [first, replacement]);

            CottonSyncedFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal("report-new.pdf", loaded.FileName);
            Assert.Equal("\"etag-2\"", loaded.ETag);
        }

        [Fact]
        public async Task Save_filters_duplicate_relative_paths_by_last_entry()
        {
            Guid replacementFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            CottonSyncedFileSnapshot first = CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"");
            CottonSyncedFileSnapshot replacement = CreateSyncedFile(replacementFileId, "report.pdf", "\"etag-2\"");

            await _store.SaveAsync(InstanceUri, _syncRoot, [first, replacement]);

            CottonSyncedFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal(replacementFileId, loaded.FileId);
            Assert.Equal("report.pdf", loaded.RelativePath);
            Assert.Equal("\"etag-2\"", loaded.ETag);
        }

        [Fact]
        public async Task Load_returns_empty_when_metadata_is_missing()
        {
            IReadOnlyList<CottonSyncedFileSnapshot> loaded = await _store.LoadAsync(InstanceUri, _syncRoot);

            Assert.Empty(loaded);
        }

        [Fact]
        public async Task Load_deletes_corrupt_metadata_and_returns_empty()
        {
            string metadataPath = CreateMetadataPath(InstanceUri, _syncRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(metadataPath, "{ not valid json");

            IReadOnlyList<CottonSyncedFileSnapshot> loaded = await _store.LoadAsync(InstanceUri, _syncRoot);

            Assert.Empty(loaded);
            Assert.False(File.Exists(metadataPath));
        }

        [Fact]
        public async Task Load_deletes_manifest_for_wrong_sync_root()
        {
            string metadataPath = CreateMetadataPath(InstanceUri, _syncRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(
                metadataPath,
                """
                {
                  "schemaVersion": 2,
                  "syncRootStableKey": "wrong-root",
                  "savedAtUtc": "2026-06-20T14:00:00Z",
                  "items": []
                }
                """);

            IReadOnlyList<CottonSyncedFileSnapshot> loaded = await _store.LoadAsync(InstanceUri, _syncRoot);

            Assert.Empty(loaded);
            Assert.False(File.Exists(metadataPath));
        }

        [Fact]
        public async Task Load_filters_invalid_records_without_discarding_valid_items()
        {
            string metadataPath = CreateMetadataPath(InstanceUri, _syncRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(
                metadataPath,
                $$"""
                {
                  "schemaVersion": 2,
                  "syncRootStableKey": "{{_syncRoot.StableKey}}",
                  "savedAtUtc": "2026-06-20T14:00:00Z",
                  "items": [
                    {
                      "fileId": "00000000-0000-0000-0000-000000000000",
                      "fileName": "bad.pdf",
                      "relativePath": "bad.pdf",
                      "eTag": "\"bad\"",
                      "remoteUpdatedAtUtc": "2026-06-20T14:00:00Z",
                      "syncedAtUtc": "2026-06-20T14:05:00Z"
                    },
                    {
                      "fileId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
                      "fileName": "good.pdf",
                      "relativePath": "Nested/good.pdf",
                      "eTag": "\"etag-1\"",
                      "remoteUpdatedAtUtc": "2026-06-20T14:00:00Z",
                      "sizeBytes": 2048,
                      "contentType": "application/pdf",
                      "syncedAtUtc": "2026-06-20T14:05:00Z"
                    }
                  ]
                }
                """);

            CottonSyncedFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));

            Assert.Equal(FileId, loaded.FileId);
            Assert.Equal("good.pdf", loaded.FileName);
            Assert.Equal("Nested/good.pdf", loaded.RelativePath);
        }

        [Fact]
        public async Task Remove_deletes_single_manifest_item()
        {
            Guid otherFileId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            await _store.SaveAsync(InstanceUri, _syncRoot, [
                CreateSyncedFile(FileId, "report.pdf", "\"etag-1\""),
                CreateSyncedFile(otherFileId, "photo.jpg", "\"etag-2\""),
            ]);

            bool removed = await _store.RemoveAsync(InstanceUri, _syncRoot, FileId);

            Assert.True(removed);
            CottonSyncedFileSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
            Assert.Equal(otherFileId, loaded.FileId);
        }

        [Fact]
        public async Task Remove_returns_false_when_file_is_missing()
        {
            await _store.SaveAsync(InstanceUri, _syncRoot, [CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"")]);

            bool removed = await _store.RemoveAsync(
                InstanceUri,
                _syncRoot,
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            Assert.False(removed);
            Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
        }

        [Fact]
        public async Task Store_isolates_manifest_by_sync_root()
        {
            CottonSyncRootSnapshot otherRoot = CreateRoot("other-sync-root");
            await _store.SaveAsync(InstanceUri, _syncRoot, [CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"")]);
            await _store.SaveAsync(InstanceUri, otherRoot, [
                CreateSyncedFile(
                    Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    "other.pdf",
                    "\"etag-2\""),
            ]);

            CottonSyncedFileSnapshot first = Assert.Single(await _store.LoadAsync(InstanceUri, _syncRoot));
            CottonSyncedFileSnapshot second = Assert.Single(await _store.LoadAsync(InstanceUri, otherRoot));

            Assert.Equal("report.pdf", first.FileName);
            Assert.Equal("other.pdf", second.FileName);
            Assert.NotEqual(
                Path.GetDirectoryName(CreateMetadataPath(InstanceUri, _syncRoot)),
                Path.GetDirectoryName(CreateMetadataPath(InstanceUri, otherRoot)));
        }

        [Fact]
        public async Task Clear_removes_manifest_metadata()
        {
            await _store.SaveAsync(InstanceUri, _syncRoot, [CreateSyncedFile(FileId, "report.pdf", "\"etag-1\"")]);

            await _store.ClearAsync(InstanceUri, _syncRoot);

            Assert.False(File.Exists(CreateMetadataPath(InstanceUri, _syncRoot)));
            Assert.Empty(await _store.LoadAsync(InstanceUri, _syncRoot));
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }

        private static CottonSyncedFileSnapshot CreateSyncedFile(Guid fileId, string fileName, string eTag)
        {
            return new CottonSyncedFileSnapshot(
                fileId,
                fileName,
                eTag,
                RemoteUpdatedAt,
                2048,
                "application/pdf",
                SyncedAt);
        }

        private static CottonSyncRootSnapshot CreateRoot(string localRootKey)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    CloudFolderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    localRootKey,
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice);
        }

        private string CreateMetadataPath(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return Path.Combine(CreateRootDirectory(instanceUri, root), FileSystemCottonSyncedFileManifestStore.MetadataFileName);
        }

        private string CreateRootDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return Path.Combine(_rootDirectory, instanceUri.Host, root.StableKey);
        }

        private class FixedSyncedFileManifestPathProvider : ICottonSyncedFileManifestPathProvider
        {
            private readonly string _rootDirectory;

            public FixedSyncedFileManifestPathProvider(string rootDirectory)
            {
                _rootDirectory = rootDirectory;
            }

            public string CreateSyncedFileManifestDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
            {
                return Path.Combine(_rootDirectory, instanceUri.Host, root.StableKey);
            }
        }
    }
}
