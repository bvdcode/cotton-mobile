// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonSyncRootStore : ICottonSyncRootStore
    {
        private const int SchemaVersion = 1;
        private const string TemporaryFileExtension = ".tmp";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public const string MetadataFileName = "sync-roots.json";

        private readonly ICottonSyncRootMetadataPathProvider _pathProvider;

        public FileSystemCottonSyncRootStore(ICottonSyncRootMetadataPathProvider pathProvider)
        {
            ArgumentNullException.ThrowIfNull(pathProvider);

            _pathProvider = pathProvider;
        }

        public async Task<IReadOnlyList<CottonSyncRootSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            string filePath = CreateMetadataFilePath(instanceUri);
            if (!File.Exists(filePath))
            {
                return [];
            }

            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 16384,
                    useAsync: true);
                CottonStoredSyncRootCollection? stored =
                    await JsonSerializer.DeserializeAsync<CottonStoredSyncRootCollection>(
                        stream,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                if (stored is null
                    || stored.SchemaVersion != SchemaVersion
                    || stored.Items is null)
                {
                    DeleteFile(filePath);
                    return [];
                }

                return Deduplicate(stored.Items
                    .Select(item => TryCreateSyncRoot(instanceUri, item))
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .ToList());
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                DeleteFile(filePath);
                return [];
            }
        }

        public async Task SaveAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonSyncRootSnapshot> roots,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(roots);
            EnsureRootsMatchInstance(instanceUri, roots);

            string directory = _pathProvider.CreateSyncRootMetadataDirectory(instanceUri);
            string filePath = Path.Combine(directory, MetadataFileName);
            string temporaryFilePath = CreateTemporaryFilePath(filePath);

            try
            {
                Directory.CreateDirectory(directory);
                await using (var stream = new FileStream(
                    temporaryFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 16384,
                    useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        CreateStoredCollection(roots),
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryFilePath, filePath, overwrite: true);
            }
            catch (OperationCanceledException)
            {
                DeleteFile(temporaryFilePath);
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                DeleteFile(temporaryFilePath);
            }
        }

        public async Task AddOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            EnsureRootsMatchInstance(instanceUri, [root]);

            IReadOnlyList<CottonSyncRootSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonSyncRootSnapshot> updated = current
                .Where(existing => existing.Id != root.Id)
                .Where(existing => !string.Equals(existing.StableKey, root.StableKey, StringComparison.Ordinal))
                .ToList();
            updated.Add(root);

            await SaveAsync(instanceUri, updated, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> RemoveAsync(
            Uri instanceUri,
            Guid rootId,
            CancellationToken cancellationToken = default)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            IReadOnlyList<CottonSyncRootSnapshot> current =
                await LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<CottonSyncRootSnapshot> updated = current
                .Where(root => root.Id != rootId)
                .ToList();
            if (updated.Count == current.Count)
            {
                return false;
            }

            await SaveAsync(instanceUri, updated, cancellationToken).ConfigureAwait(false);
            return true;
        }

        public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            DeleteFile(CreateMetadataFilePath(instanceUri));
            return Task.CompletedTask;
        }

        private string CreateMetadataFilePath(Uri instanceUri)
        {
            return Path.Combine(_pathProvider.CreateSyncRootMetadataDirectory(instanceUri), MetadataFileName);
        }

        private static CottonStoredSyncRootCollection CreateStoredCollection(
            IReadOnlyCollection<CottonSyncRootSnapshot> roots)
        {
            return new CottonStoredSyncRootCollection
            {
                SchemaVersion = SchemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                Items = Deduplicate(roots)
                    .Select(CreateStoredItem)
                    .ToList(),
            };
        }

        private static CottonStoredSyncRootItem CreateStoredItem(CottonSyncRootSnapshot root)
        {
            return new CottonStoredSyncRootItem
            {
                Id = root.Id,
                InstanceUri = root.InstanceUri.AbsoluteUri,
                AccountScopeKey = root.AccountScopeKey,
                CloudFolderId = root.CloudFolder.FolderId,
                CloudFolderName = root.CloudFolder.FolderName,
                CloudFolderPath = root.CloudFolder.Path,
                LocalStorageKind = root.LocalRoot.StorageKind,
                LocalRootKey = root.LocalRoot.RootKey,
                LocalRootDisplayName = root.LocalRoot.DisplayName,
                LocalPermissionStatus = root.LocalRoot.PermissionStatus,
                Direction = root.Direction,
                StableKey = root.StableKey,
            };
        }

        private static CottonSyncRootSnapshot? TryCreateSyncRoot(Uri expectedInstanceUri, CottonStoredSyncRootItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.InstanceUri)
                    || !Uri.TryCreate(item.InstanceUri, UriKind.Absolute, out Uri? storedInstanceUri))
                {
                    return null;
                }

                var root = new CottonSyncRootSnapshot(
                    item.Id,
                    storedInstanceUri,
                    item.AccountScopeKey ?? string.Empty,
                    new CottonUploadDestinationSnapshot(
                        item.CloudFolderId,
                        item.CloudFolderName ?? string.Empty,
                        item.CloudFolderPath),
                    new CottonSyncLocalRootSnapshot(
                        item.LocalStorageKind,
                        item.LocalRootKey ?? string.Empty,
                        item.LocalRootDisplayName ?? string.Empty,
                        item.LocalPermissionStatus),
                    item.Direction);
                if (!IsSameInstance(root.InstanceUri, expectedInstanceUri)
                    || string.IsNullOrWhiteSpace(item.StableKey)
                    || !string.Equals(root.StableKey, item.StableKey.Trim(), StringComparison.Ordinal))
                {
                    return null;
                }

                return root;
            }
            catch (Exception exception)
                when (exception is ArgumentException or ArgumentOutOfRangeException or UriFormatException)
            {
                return null;
            }
        }

        private static void EnsureRootsMatchInstance(
            Uri instanceUri,
            IReadOnlyCollection<CottonSyncRootSnapshot> roots)
        {
            foreach (CottonSyncRootSnapshot root in roots)
            {
                if (!IsSameInstance(root.InstanceUri, instanceUri))
                {
                    throw new ArgumentException("Sync roots must match the metadata instance.", nameof(roots));
                }
            }
        }

        private static List<CottonSyncRootSnapshot> Deduplicate(
            IReadOnlyCollection<CottonSyncRootSnapshot> roots)
        {
            return roots
                .GroupBy(root => root.Id)
                .Select(group => group.Last())
                .GroupBy(root => root.StableKey, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToList();
        }

        private static bool IsSameInstance(Uri first, Uri second)
        {
            return string.Equals(
                NormalizeInstanceUri(first).AbsoluteUri,
                NormalizeInstanceUri(second).AbsoluteUri,
                StringComparison.Ordinal);
        }

        private static Uri NormalizeInstanceUri(Uri instanceUri)
        {
            if (!instanceUri.IsAbsoluteUri
                || !string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(instanceUri.Host)
                || !string.IsNullOrWhiteSpace(instanceUri.UserInfo)
                || !string.IsNullOrWhiteSpace(instanceUri.Query)
                || !string.IsNullOrWhiteSpace(instanceUri.Fragment))
            {
                throw new ArgumentException("Sync root instance URI must be an absolute HTTPS URL.", nameof(instanceUri));
            }

            var builder = new UriBuilder(instanceUri)
            {
                Scheme = instanceUri.Scheme.ToLowerInvariant(),
                Host = instanceUri.Host.ToLowerInvariant(),
            };

            if (builder.Uri.IsDefaultPort)
            {
                builder.Port = -1;
            }

            string path = builder.Path.TrimEnd('/');
            builder.Path = string.IsNullOrWhiteSpace(path) ? "/" : path;
            return builder.Uri;
        }

        private static string CreateTemporaryFilePath(string filePath)
        {
            return $"{filePath}.{Guid.NewGuid():N}{TemporaryFileExtension}";
        }

        private static void DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
