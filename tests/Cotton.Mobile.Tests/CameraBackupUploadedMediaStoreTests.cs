using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CameraBackupUploadedMediaStoreTests : IDisposable
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Uri OtherInstanceUri = new("https://files.cottoncloud.dev");
        private static readonly DateTime ModifiedAt = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime UploadedAt = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
        private static readonly Guid RemoteFileId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        private readonly string _rootDirectory;
        private readonly FileSystemCottonCameraBackupUploadedMediaStore _store;

        public CameraBackupUploadedMediaStoreTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "cotton-camera-backup-uploaded-tests",
                Guid.NewGuid().ToString("N"));
            _store = new FileSystemCottonCameraBackupUploadedMediaStore(
                new FixedCameraBackupMetadataPathProvider(_rootDirectory));
        }

        [Fact]
        public async Task Save_and_load_roundtrips_uploaded_media_identity()
        {
            CottonCameraBackupUploadedMediaSnapshot uploaded = CreateUploadedMedia(
                "media://photo/1",
                RemoteFileId,
                "photo.jpg");

            await _store.SaveAsync(InstanceUri, [uploaded]);

            CottonCameraBackupUploadedMediaSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal("media://photo/1", loaded.Identity.SourceId);
            Assert.Equal(ModifiedAt, loaded.Identity.LastModifiedUtc);
            Assert.Equal(1024, loaded.Identity.SizeBytes);
            Assert.Equal(UploadedAt, loaded.UploadedAtUtc);
            Assert.Equal(RemoteFileId, loaded.RemoteFileId);
            Assert.Equal("photo.jpg", loaded.RemoteFileName);
        }

        [Fact]
        public async Task Add_or_replace_updates_existing_identity()
        {
            CottonCameraBackupUploadedMediaSnapshot first = CreateUploadedMedia(
                "media://photo/1",
                RemoteFileId,
                "photo.jpg");
            CottonCameraBackupUploadedMediaSnapshot replacement = CreateUploadedMedia(
                "media://photo/1",
                Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                "photo-renamed.jpg");

            await _store.AddOrReplaceAsync(InstanceUri, first);
            await _store.AddOrReplaceAsync(InstanceUri, replacement);

            CottonCameraBackupUploadedMediaSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal("photo-renamed.jpg", loaded.RemoteFileName);
            Assert.Equal(Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"), loaded.RemoteFileId);
        }

        [Fact]
        public async Task Save_filters_duplicate_identities_by_last_entry()
        {
            CottonCameraBackupUploadedMediaSnapshot first = CreateUploadedMedia(
                "media://photo/1",
                RemoteFileId,
                "photo.jpg");
            CottonCameraBackupUploadedMediaSnapshot replacement = CreateUploadedMedia(
                "media://photo/1",
                Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                "photo-new.jpg");

            await _store.SaveAsync(InstanceUri, [first, replacement]);

            CottonCameraBackupUploadedMediaSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));
            Assert.Equal("photo-new.jpg", loaded.RemoteFileName);
        }

        [Fact]
        public async Task Load_returns_empty_when_metadata_is_missing()
        {
            IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> loaded =
                await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
        }

        [Fact]
        public async Task Load_deletes_corrupt_metadata_and_returns_empty()
        {
            string metadataPath = CreateMetadataPath(InstanceUri);
            Directory.CreateDirectory(Path.GetDirectoryName(metadataPath)!);
            await File.WriteAllTextAsync(metadataPath, "{ not valid json");

            IReadOnlyList<CottonCameraBackupUploadedMediaSnapshot> loaded =
                await _store.LoadAsync(InstanceUri);

            Assert.Empty(loaded);
            Assert.False(File.Exists(metadataPath));
        }

        [Fact]
        public async Task Load_filters_invalid_records_without_discarding_valid_records()
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
                      "sourceId": " ",
                      "uploadedAtUtc": "2026-06-19T12:00:00Z"
                    },
                    {
                      "sourceId": "media://photo/1",
                      "lastModifiedUtc": "2026-06-19T10:00:00Z",
                      "sizeBytes": 1024,
                      "uploadedAtUtc": "2026-06-19T12:00:00Z",
                      "remoteFileId": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                      "remoteFileName": "photo.jpg"
                    }
                  ]
                }
                """);

            CottonCameraBackupUploadedMediaSnapshot loaded = Assert.Single(await _store.LoadAsync(InstanceUri));

            Assert.Equal("media://photo/1", loaded.Identity.SourceId);
            Assert.Equal("photo.jpg", loaded.RemoteFileName);
        }

        [Fact]
        public async Task Store_isolates_uploaded_media_by_instance()
        {
            await _store.SaveAsync(InstanceUri, [CreateUploadedMedia("media://photo/1", RemoteFileId, "photo.jpg")]);
            await _store.SaveAsync(OtherInstanceUri, [
                CreateUploadedMedia(
                    "media://photo/2",
                    Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                    "other.jpg"),
            ]);

            CottonCameraBackupUploadedMediaSnapshot first =
                Assert.Single(await _store.LoadAsync(InstanceUri));
            CottonCameraBackupUploadedMediaSnapshot second =
                Assert.Single(await _store.LoadAsync(OtherInstanceUri));

            Assert.Equal("media://photo/1", first.Identity.SourceId);
            Assert.Equal("media://photo/2", second.Identity.SourceId);
            Assert.NotEqual(
                Path.GetDirectoryName(CreateMetadataPath(InstanceUri)),
                Path.GetDirectoryName(CreateMetadataPath(OtherInstanceUri)));
        }

        [Fact]
        public async Task Clear_removes_uploaded_media_metadata()
        {
            await _store.SaveAsync(InstanceUri, [CreateUploadedMedia("media://photo/1", RemoteFileId, "photo.jpg")]);

            await _store.ClearAsync(InstanceUri);

            Assert.False(File.Exists(CreateMetadataPath(InstanceUri)));
            Assert.Empty(await _store.LoadAsync(InstanceUri));
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }

        private static CottonCameraBackupUploadedMediaSnapshot CreateUploadedMedia(
            string sourceId,
            Guid remoteFileId,
            string remoteFileName)
        {
            return new CottonCameraBackupUploadedMediaSnapshot(
                new CottonCameraBackupMediaIdentity(sourceId, ModifiedAt, 1024),
                UploadedAt,
                remoteFileId,
                remoteFileName);
        }

        private string CreateMetadataPath(Uri instanceUri)
        {
            return Path.Combine(CreateInstanceDirectory(instanceUri), "uploaded-media.json");
        }

        private string CreateInstanceDirectory(Uri instanceUri)
        {
            return Path.Combine(_rootDirectory, instanceUri.Host);
        }

        private class FixedCameraBackupMetadataPathProvider : ICottonCameraBackupMetadataPathProvider
        {
            private readonly string _rootDirectory;

            public FixedCameraBackupMetadataPathProvider(string rootDirectory)
            {
                _rootDirectory = rootDirectory;
            }

            public string CreateCameraBackupMetadataDirectory(Uri instanceUri)
            {
                return Path.Combine(_rootDirectory, instanceUri.Host);
            }
        }
    }
}
