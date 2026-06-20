using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Uri OtherInstanceUri = new("https://cloud.example.test");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid OtherFolderId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _store;

        public SyncRootStoreTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-sync-root-store-tests", Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonSyncRootStore(new FixedSyncRootMetadataPathProvider(_directory));
        }

        [Fact]
        public async Task Save_and_load_roundtrips_app_private_sync_root()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, FolderId, "Projects");

            await _store.SaveAsync(InstanceUri, [root]);

            CottonSyncRootSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(root.Id, loaded.Id);
            Assert.Equal(root.InstanceUri, loaded.InstanceUri);
            Assert.Equal(root.AccountScopeKey, loaded.AccountScopeKey);
            Assert.Equal(root.CloudFolder.FolderId, loaded.CloudFolder.FolderId);
            Assert.Equal(root.CloudFolder.Path, loaded.CloudFolder.Path);
            Assert.Equal(root.LocalRoot.StorageKind, loaded.LocalRoot.StorageKind);
            Assert.Equal(root.LocalRoot.RootKey, loaded.LocalRoot.RootKey);
            Assert.Equal(root.LocalRoot.PermissionStatus, loaded.LocalRoot.PermissionStatus);
            Assert.Equal(root.Direction, loaded.Direction);
            Assert.Equal(root.StableKey, loaded.StableKey);
        }

        [Fact]
        public async Task Add_or_replace_replaces_existing_root_by_id()
        {
            CottonSyncRootSnapshot original = CreateRoot(RootId, FolderId, "Projects");
            CottonSyncRootSnapshot replacement = CreateRoot(RootId, OtherFolderId, "Archive");

            await _store.AddOrReplaceAsync(InstanceUri, original);
            await _store.AddOrReplaceAsync(InstanceUri, replacement);

            CottonSyncRootSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(OtherFolderId, loaded.CloudFolder.FolderId);
            Assert.Equal("Files / Archive", loaded.CloudFolder.Path);
        }

        [Fact]
        public async Task Add_or_replace_replaces_existing_root_by_stable_key()
        {
            CottonSyncRootSnapshot original = CreateRoot(RootId, FolderId, "Projects");
            CottonSyncRootSnapshot replacement =
                CreateRoot(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), FolderId, "Projects");

            await _store.AddOrReplaceAsync(InstanceUri, original);
            await _store.AddOrReplaceAsync(InstanceUri, replacement);

            CottonSyncRootSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(replacement.Id, loaded.Id);
            Assert.Equal(original.StableKey, loaded.StableKey);
        }

        [Fact]
        public async Task Remove_deletes_root_by_id()
        {
            await _store.SaveAsync(
                InstanceUri,
                [
                    CreateRoot(RootId, FolderId, "Projects"),
                    CreateRoot(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), OtherFolderId, "Archive"),
                ]);

            bool removed = await _store.RemoveAsync(InstanceUri, RootId);

            Assert.True(removed);
            CottonSyncRootSnapshot remaining = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(OtherFolderId, remaining.CloudFolder.FolderId);
        }

        [Fact]
        public async Task Remove_returns_false_when_root_is_missing()
        {
            bool removed = await _store.RemoveAsync(InstanceUri, RootId);

            Assert.False(removed);
        }

        [Fact]
        public async Task Load_returns_empty_list_when_metadata_file_is_missing()
        {
            IReadOnlyList<CottonSyncRootSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
        }

        [Fact]
        public async Task Load_deletes_corrupt_metadata_file_and_returns_empty_list()
        {
            Directory.CreateDirectory(_directory);
            string metadataPath = CreateMetadataPath();
            await File.WriteAllTextAsync(metadataPath, "{ not valid json");

            IReadOnlyList<CottonSyncRootSnapshot> loaded = await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
            Assert.False(File.Exists(metadataPath));
        }

        [Fact]
        public async Task Load_filters_invalid_records_without_discarding_valid_roots()
        {
            CottonSyncRootSnapshot root = CreateRoot(RootId, FolderId, "Projects");
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(
                CreateMetadataPath(),
                $$"""
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-20T09:00:00Z",
                  "items": [
                    {
                      "id": "00000000-0000-0000-0000-000000000000",
                      "instanceUri": "{{InstanceUri.AbsoluteUri}}",
                      "accountScopeKey": "account-1",
                      "cloudFolderId": "{{FolderId:D}}",
                      "cloudFolderName": "Projects",
                      "cloudFolderPath": "Files / Projects",
                      "localStorageKind": 0,
                      "localRootKey": "app-private-sync-root",
                      "localRootDisplayName": "On this device",
                      "localPermissionStatus": 0,
                      "direction": 0,
                      "stableKey": "invalid"
                    },
                    {
                      "id": "{{root.Id:D}}",
                      "instanceUri": "{{root.InstanceUri.AbsoluteUri}}",
                      "accountScopeKey": "{{root.AccountScopeKey}}",
                      "cloudFolderId": "{{root.CloudFolder.FolderId:D}}",
                      "cloudFolderName": "{{root.CloudFolder.FolderName}}",
                      "cloudFolderPath": "{{root.CloudFolder.Path}}",
                      "localStorageKind": 0,
                      "localRootKey": "{{root.LocalRoot.RootKey}}",
                      "localRootDisplayName": "{{root.LocalRoot.DisplayName}}",
                      "localPermissionStatus": 0,
                      "direction": 0,
                      "stableKey": "{{root.StableKey}}"
                    }
                  ]
                }
                """);

            CottonSyncRootSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));

            Assert.Equal(root.Id, loaded.Id);
            Assert.Equal(root.StableKey, loaded.StableKey);
        }

        [Fact]
        public async Task Load_ignores_roots_for_another_instance()
        {
            CottonSyncRootSnapshot current = CreateRoot(RootId, FolderId, "Projects");
            CottonSyncRootSnapshot other = CreateRoot(
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                OtherFolderId,
                "Archive",
                OtherInstanceUri);
            Directory.CreateDirectory(_directory);
            await File.WriteAllTextAsync(
                CreateMetadataPath(),
                $$"""
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-20T09:00:00Z",
                  "items": [
                    {
                      "id": "{{other.Id:D}}",
                      "instanceUri": "{{other.InstanceUri.AbsoluteUri}}",
                      "accountScopeKey": "{{other.AccountScopeKey}}",
                      "cloudFolderId": "{{other.CloudFolder.FolderId:D}}",
                      "cloudFolderName": "{{other.CloudFolder.FolderName}}",
                      "cloudFolderPath": "{{other.CloudFolder.Path}}",
                      "localStorageKind": 0,
                      "localRootKey": "{{other.LocalRoot.RootKey}}",
                      "localRootDisplayName": "{{other.LocalRoot.DisplayName}}",
                      "localPermissionStatus": 0,
                      "direction": 0,
                      "stableKey": "{{other.StableKey}}"
                    },
                    {
                      "id": "{{current.Id:D}}",
                      "instanceUri": "{{current.InstanceUri.AbsoluteUri}}",
                      "accountScopeKey": "{{current.AccountScopeKey}}",
                      "cloudFolderId": "{{current.CloudFolder.FolderId:D}}",
                      "cloudFolderName": "{{current.CloudFolder.FolderName}}",
                      "cloudFolderPath": "{{current.CloudFolder.Path}}",
                      "localStorageKind": 0,
                      "localRootKey": "{{current.LocalRoot.RootKey}}",
                      "localRootDisplayName": "{{current.LocalRoot.DisplayName}}",
                      "localPermissionStatus": 0,
                      "direction": 0,
                      "stableKey": "{{current.StableKey}}"
                    }
                  ]
                }
                """);

            CottonSyncRootSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal(current.Id, loaded.Id);
        }

        [Fact]
        public async Task Save_rejects_roots_for_another_instance()
        {
            CottonSyncRootSnapshot other = CreateRoot(RootId, FolderId, "Projects", OtherInstanceUri);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _store.SaveAsync(InstanceUri, [other]));
        }

        [Fact]
        public async Task Clear_removes_metadata_file()
        {
            await _store.SaveAsync(InstanceUri, [CreateRoot(RootId, FolderId, "Projects")]);

            await _store.ClearAsync(InstanceUri);

            Assert.False(File.Exists(CreateMetadataPath()));
            Assert.Empty(await _store.LoadAsync(InstanceUri));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            Guid folderId,
            string folderName,
            Uri? instanceUri = null)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                instanceUri ?? InstanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    folderId,
                    folderName,
                    $"Files / {folderName}"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-sync-root",
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice);
        }

        private string CreateMetadataPath()
        {
            return Path.Combine(_directory, FileSystemCottonSyncRootStore.MetadataFileName);
        }

        private class FixedSyncRootMetadataPathProvider : ICottonSyncRootMetadataPathProvider
        {
            private readonly string _directory;

            public FixedSyncRootMetadataPathProvider(string directory)
            {
                _directory = directory;
            }

            public string CreateSyncRootMetadataDirectory(Uri instanceUri)
            {
                return _directory;
            }
        }
    }
}
