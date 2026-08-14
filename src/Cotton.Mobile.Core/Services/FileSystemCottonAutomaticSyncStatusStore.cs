// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileSystemCottonAutomaticSyncStatusStore(
        ICottonSyncRootMetadataPathProvider pathProvider,
        ILogger<FileSystemCottonAutomaticSyncStatusStore> logger,
        TimeProvider timeProvider) : ICottonAutomaticSyncStatusStore, IDisposable
    {
        private const int SchemaVersion = 1;
        public const string MetadataFileName = "automatic-sync-status.json";

        private static readonly Action<ILogger, string, Exception?> LogLoadFailed = LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, nameof(LoadAsync)),
            "Failed to load automatic sync status from {FilePath}; resetting the store.");
        private static readonly Action<ILogger, string, Exception?> LogSaveFailed = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, nameof(UpdateAsync)),
            "Failed to save automatic sync status to {FilePath}.");

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly ICottonSyncRootMetadataPathProvider _pathProvider =
            pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        private readonly ILogger<FileSystemCottonAutomaticSyncStatusStore> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public event EventHandler<CottonAutomaticSyncStatusesChangedEventArgs>? StatusesChanged;

        public async Task<IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>> LoadAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await LoadCoreAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task UpdateAsync(
            Uri instanceUri,
            IReadOnlySet<Guid> activeRootIds,
            IReadOnlyCollection<CottonAutomaticSyncRootStatusSnapshot> updatedStatuses,
            CancellationToken cancellationToken = default)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ValidateUpdate(activeRootIds, updatedStatuses);

            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> result;
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> current =
                    await LoadCoreAsync(instanceUri, cancellationToken).ConfigureAwait(false);
                Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> merged = current
                    .Where(item => activeRootIds.Contains(item.Key))
                    .ToDictionary();
                foreach (CottonAutomaticSyncRootStatusSnapshot status in updatedStatuses)
                {
                    merged[status.RootId] = status;
                }

                result = AsReadOnly(merged);
                await SaveCoreAsync(instanceUri, [.. result.Values], cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }

            StatusesChanged?.Invoke(
                this,
                new CottonAutomaticSyncStatusesChangedEventArgs(instanceUri, result));
        }

        public void Dispose()
        {
            _gate.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task<IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>> LoadCoreAsync(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            string filePath = CreateMetadataFilePath(instanceUri);
            if (!File.Exists(filePath))
            {
                return Empty();
            }

            try
            {
                CottonStoredAutomaticSyncStatusCollection? stored = await CottonAtomicJsonFile
                    .ReadAsync<CottonStoredAutomaticSyncStatusCollection>(filePath, cancellationToken)
                    .ConfigureAwait(false);
                if (stored is null || stored.SchemaVersion != SchemaVersion || stored.Items is null)
                {
                    CottonAtomicJsonFile.DeleteIfExists(filePath);
                    return Empty();
                }

                Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses = [];
                foreach (CottonStoredAutomaticSyncRootStatus? item in stored.Items)
                {
                    CottonAutomaticSyncRootStatusSnapshot? status = CreateStatus(item);
                    if (status is not null)
                    {
                        statuses[status.RootId] = status;
                    }
                }

                return AsReadOnly(statuses);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                LogLoadFailed(_logger, filePath, exception);
                CottonAtomicJsonFile.DeleteIfExists(filePath);
                return Empty();
            }
        }

        private async Task SaveCoreAsync(
            Uri instanceUri,
            IReadOnlyCollection<CottonAutomaticSyncRootStatusSnapshot> statuses,
            CancellationToken cancellationToken)
        {
            string filePath = CreateMetadataFilePath(instanceUri);
            if (statuses.Count == 0)
            {
                CottonAtomicJsonFile.DeleteIfExists(filePath);
                return;
            }

            try
            {
                CottonStoredAutomaticSyncStatusCollection stored = new()
                {
                    SchemaVersion = SchemaVersion,
                    SavedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                    Items = [.. statuses
                        .OrderBy(status => status.RootId)
                        .Select(CreateStoredStatus)],
                };
                await CottonAtomicJsonFile.WriteAsync(filePath, stored, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                LogSaveFailed(_logger, filePath, exception);
                throw;
            }
        }

        private static void ValidateUpdate(
            IReadOnlySet<Guid> activeRootIds,
            IReadOnlyCollection<CottonAutomaticSyncRootStatusSnapshot> updatedStatuses)
        {
            ArgumentNullException.ThrowIfNull(activeRootIds);
            ArgumentNullException.ThrowIfNull(updatedStatuses);
            if (activeRootIds.Contains(Guid.Empty))
            {
                throw new ArgumentException("Active sync root ids cannot be empty.", nameof(activeRootIds));
            }

            foreach (CottonAutomaticSyncRootStatusSnapshot status in updatedStatuses)
            {
                ArgumentNullException.ThrowIfNull(status);
                if (!activeRootIds.Contains(status.RootId))
                {
                    throw new ArgumentException(
                        "Updated automatic sync statuses must belong to active roots.",
                        nameof(updatedStatuses));
                }
            }
        }

        private static CottonAutomaticSyncRootStatusSnapshot? CreateStatus(
            CottonStoredAutomaticSyncRootStatus? item)
        {
            if (item is null
                || item.RootId == Guid.Empty
                || !Enum.IsDefined(item.Outcome)
                || item.CompletedAtUtc.Kind != DateTimeKind.Utc)
            {
                return null;
            }

            return new CottonAutomaticSyncRootStatusSnapshot(
                item.RootId,
                item.Outcome,
                item.CompletedAtUtc);
        }

        private static CottonStoredAutomaticSyncRootStatus CreateStoredStatus(
            CottonAutomaticSyncRootStatusSnapshot status)
        {
            return new CottonStoredAutomaticSyncRootStatus
            {
                RootId = status.RootId,
                Outcome = status.Outcome,
                CompletedAtUtc = status.CompletedAtUtc,
            };
        }

        private static ReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> AsReadOnly(
            IDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> statuses)
        {
            return new ReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>(
                new Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>(statuses));
        }

        private static ReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> Empty()
        {
            return new ReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>(
                new Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>());
        }

        private string CreateMetadataFilePath(Uri instanceUri)
        {
            return Path.Combine(_pathProvider.CreateSyncRootMetadataDirectory(instanceUri), MetadataFileName);
        }
    }
}
