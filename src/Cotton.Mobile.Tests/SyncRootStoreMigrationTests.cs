using System.Text.Json;
using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.TestFilePaths;

namespace Cotton.Mobile.Tests
{
    public class SyncRootStoreMigrationTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private readonly string _directory;
        private readonly FileSystemCottonSyncRootStore _store;

        public SyncRootStoreMigrationTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cotton-sync-root-migration", Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonSyncRootStore(
                new FixedSyncRootMetadataPathProvider(_directory),
                NullLogger<FileSystemCottonSyncRootStore>.Instance,
                TimeProvider.System);
        }

        [Fact]
        public async Task LoadMigratesUploadRootsAndRemovesUnsupportedLegacyDirections()
        {
            CottonSyncRootSnapshot uploadRoot = SyncTestRootFactory.CreateDocumentTreeRoot();
            Directory.CreateDirectory(_directory);
            string metadataPath = CreateSyncRootMetadataPath(_directory);
            await File.WriteAllTextAsync(
                metadataPath,
                $$"""
                {
                  "schemaVersion": 1,
                  "savedAtUtc": "2026-06-20T09:00:00Z",
                  "items": [
                    {
                      "id": "{{Guid.NewGuid():D}}",
                      "direction": 0
                    },
                    {
                      "id": "{{uploadRoot.Id:D}}",
                      "instanceUri": "{{uploadRoot.InstanceUri.AbsoluteUri}}",
                      "accountScopeKey": "{{uploadRoot.AccountScopeKey}}",
                      "cloudFolderId": "{{uploadRoot.CloudFolder.FolderId:D}}",
                      "cloudFolderName": "{{uploadRoot.CloudFolder.FolderName}}",
                      "cloudFolderPath": "{{uploadRoot.CloudFolder.Path}}",
                      "localStorageKind": 1,
                      "localRootKey": "{{uploadRoot.LocalRoot.RootKey}}",
                      "localRootDisplayName": "{{uploadRoot.LocalRoot.DisplayName}}",
                      "localPermissionStatus": 0,
                      "direction": 1,
                      "stableKey": "{{uploadRoot.StableKey}}"
                    },
                    {
                      "id": "{{Guid.NewGuid():D}}",
                      "direction": 2
                    }
                  ]
                }
                """);

            CottonSyncRootSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            using JsonDocument migrated = JsonDocument.Parse(await File.ReadAllTextAsync(metadataPath));

            Assert.Equal(uploadRoot.Id, loaded.Id);
            Assert.Equal(2, migrated.RootElement.GetProperty("schemaVersion").GetInt32());
            JsonElement item = Assert.Single(migrated.RootElement.GetProperty("items").EnumerateArray());
            Assert.Equal(1, item.GetProperty("direction").GetInt32());
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
