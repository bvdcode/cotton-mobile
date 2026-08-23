// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cotton.Mobile.Services
{
    public class FileDownloadCacheProtectionProvider(
        ICottonOfflineFilePinStore offlineFilePinStore,
        ILogger<FileDownloadCacheProtectionProvider> logger)
    {
        private readonly ICottonOfflineFilePinStore _offlineFilePinStore =
            offlineFilePinStore ?? throw new ArgumentNullException(nameof(offlineFilePinStore));
        private readonly ILogger<FileDownloadCacheProtectionProvider> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<IReadOnlyCollection<string>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CottonOfflineFilePinSnapshot> pins =
                await _offlineFilePinStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            List<string> protectedDirectories = [.. pins.Select(
                pin => CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, pin.FileId))];
            protectedDirectories.AddRange(LoadManifestDirectories(instanceUri, cancellationToken));
            return protectedDirectories;
        }

        private HashSet<string> LoadManifestDirectories(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            string manifestDirectory = Path.Combine(
                CottonMobileStoragePaths.CreateSyncedFileManifestRootDirectory(),
                CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri));
            if (!Directory.Exists(manifestDirectory))
            {
                return [];
            }

            HashSet<string> protectedDirectories = new(StringComparer.Ordinal);
            try
            {
                foreach (string manifestPath in Directory.EnumerateFiles(
                    manifestDirectory,
                    FileSystemCottonSyncedFileManifestStore.MetadataFileName,
                    SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (Guid fileId in ReadFileIds(manifestPath, cancellationToken))
                    {
                        protectedDirectories.Add(
                            CottonMobileStoragePaths.CreateDownloadDirectory(instanceUri, fileId));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or InvalidOperationException)
            {
                CottonLog.DebugWithContext(
                    _logger,
                    "Failed to inspect Cotton mobile synced-file manifests.",
                    manifestDirectory,
                    exception);
            }

            return protectedDirectories;
        }

        private List<Guid> ReadFileIds(string manifestPath, CancellationToken cancellationToken)
        {
            try
            {
                using FileStream stream = File.OpenRead(manifestPath);
                using JsonDocument document = JsonDocument.Parse(stream);
                cancellationToken.ThrowIfCancellationRequested();
                if (!HasSupportedSchema(document.RootElement, out JsonElement items))
                {
                    return [];
                }

                List<Guid> fileIds = [];
                foreach (JsonElement item in items.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (item.TryGetProperty("fileId", out JsonElement element)
                        && Guid.TryParse(element.GetString(), out Guid fileId)
                        && fileId != Guid.Empty)
                    {
                        fileIds.Add(fileId);
                    }
                }

                return fileIds;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or InvalidOperationException)
            {
                CottonLog.DebugWithContext(
                    _logger,
                    "Failed to inspect a Cotton mobile synced-file manifest.",
                    manifestPath,
                    exception);
                return [];
            }
        }

        private static bool HasSupportedSchema(JsonElement root, out JsonElement items)
        {
            items = default;
            bool hasSupportedSchema = root.TryGetProperty("schemaVersion", out JsonElement schemaVersion)
                && schemaVersion.TryGetInt32(out int parsedSchemaVersion)
                && parsedSchemaVersion == CottonSyncedFileManifestSchema.CurrentVersion;
            return hasSupportedSchema
                && root.TryGetProperty("items", out items)
                && items.ValueKind == JsonValueKind.Array;
        }
    }
}
