using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TransferQueueRestoreCoordinatorTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly DateTime CreatedAt = new(2026, 6, 19, 16, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime RestoredAt = new(2026, 6, 19, 16, 5, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Restore_keeps_transfers_with_staged_files_and_saves_reconciled_queue()
        {
            CottonTransferQueueItem queued = CreateUpload(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            CottonTransferQueueItem failed = CreateUpload(Guid.Parse("22222222-2222-2222-2222-222222222222"))
                .Start(CreatedAt.AddSeconds(1))
                .Fail("Offline", CreatedAt.AddSeconds(2));
            var metadataStore = new FakeTransferMetadataStore([queued, failed]);
            var stagingStore = new FakeTransferStagingStore([queued.Id, failed.Id]);
            var coordinator = CreateCoordinator(metadataStore, stagingStore);

            IReadOnlyList<CottonTransferQueueItem> restored = await coordinator.RestoreAsync(InstanceUri);

            Assert.Equal([queued.Id, failed.Id], restored.Select(item => item.Id).ToArray());
            Assert.Equal(CottonTransferStatus.Queued, restored[0].Status);
            Assert.Equal(CottonTransferStatus.Failed, restored[1].Status);
            Assert.Equal(restored, metadataStore.SavedItems);
            Assert.Empty(stagingStore.DeletedTransferIds);
            Assert.Single(stagingStore.CleanupQueueSnapshots);
        }

        [Fact]
        public async Task Restore_marks_non_terminal_transfers_missing_staged_files_as_failed()
        {
            CottonTransferQueueItem queued = CreateUpload(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            CottonTransferQueueItem failed = CreateUpload(Guid.Parse("22222222-2222-2222-2222-222222222222"))
                .Start(CreatedAt.AddSeconds(1))
                .Fail("Offline", CreatedAt.AddSeconds(2));
            var metadataStore = new FakeTransferMetadataStore([queued, failed]);
            var stagingStore = new FakeTransferStagingStore([failed.Id]);
            var coordinator = CreateCoordinator(metadataStore, stagingStore);

            IReadOnlyList<CottonTransferQueueItem> restored = await coordinator.RestoreAsync(InstanceUri);

            CottonTransferQueueItem missing = restored.Single(item => item.Id == queued.Id);
            Assert.Equal(CottonTransferStatus.Failed, missing.Status);
            Assert.True(missing.CanRetry);
            Assert.Equal("Upload file is no longer available on this device.", missing.FailureMessage);
            Assert.Equal(RestoredAt, missing.UpdatedAtUtc);

            CottonTransferQueueItem stillFailed = restored.Single(item => item.Id == failed.Id);
            Assert.Equal("Offline", stillFailed.FailureMessage);
            Assert.Equal(CreatedAt.AddSeconds(2), stillFailed.UpdatedAtUtc);
        }

        [Fact]
        public async Task Restore_cleans_terminal_and_orphaned_staged_files()
        {
            CottonTransferQueueItem queued = CreateUpload(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            CottonTransferQueueItem completed = CreateUpload(Guid.Parse("22222222-2222-2222-2222-222222222222"))
                .Start(CreatedAt.AddSeconds(1))
                .Complete(CreatedAt.AddSeconds(2));
            CottonTransferQueueItem cancelled = CreateUpload(Guid.Parse("33333333-3333-3333-3333-333333333333"))
                .Cancel(CreatedAt.AddSeconds(3));
            Guid orphanedId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            var metadataStore = new FakeTransferMetadataStore([queued, completed, cancelled]);
            var stagingStore = new FakeTransferStagingStore([queued.Id, completed.Id, cancelled.Id, orphanedId]);
            var coordinator = CreateCoordinator(metadataStore, stagingStore);

            await coordinator.RestoreAsync(InstanceUri);

            Assert.Equal(
                new[] { cancelled.Id, completed.Id, orphanedId }.Order().ToArray(),
                stagingStore.DeletedTransferIds.Order().ToArray());
            Assert.Equal([queued.Id], stagingStore.StagedTransferIds.Order().ToArray());
        }

        [Fact]
        public async Task Restore_uses_metadata_store_restart_decisions_before_reconciliation()
        {
            CottonTransferQueueItem running = CreateUpload(Guid.Parse("11111111-1111-1111-1111-111111111111"))
                .Start(CreatedAt.AddSeconds(1));
            var metadataStore = new FakeTransferMetadataStore([running.RestoreAfterRestart(RestoredAt)]);
            var stagingStore = new FakeTransferStagingStore([running.Id]);
            var coordinator = CreateCoordinator(metadataStore, stagingStore);

            IReadOnlyList<CottonTransferQueueItem> restored = await coordinator.RestoreAsync(InstanceUri);

            CottonTransferQueueItem item = Assert.Single(restored);
            Assert.Equal(CottonTransferStatus.Queued, item.Status);
            Assert.Equal(RestoredAt, item.UpdatedAtUtc);
            Assert.Equal(restored, metadataStore.SavedItems);
        }

        private static CottonTransferQueueRestoreCoordinator CreateCoordinator(
            FakeTransferMetadataStore metadataStore,
            FakeTransferStagingStore stagingStore)
        {
            return new CottonTransferQueueRestoreCoordinator(
                metadataStore,
                stagingStore,
                new FixedTimeProvider(RestoredAt));
        }

        private static CottonTransferQueueItem CreateUpload(Guid id)
        {
            return CottonTransferQueueItem.CreateUpload(id, "upload.bin", 100, CreatedAt);
        }

        private class FakeTransferMetadataStore : ICottonTransferMetadataStore
        {
            private readonly IReadOnlyList<CottonTransferQueueItem> _items;

            public FakeTransferMetadataStore(IReadOnlyList<CottonTransferQueueItem> items)
            {
                _items = items;
            }

            public IReadOnlyList<CottonTransferQueueItem> SavedItems { get; private set; } = [];

            public Task<IReadOnlyList<CottonTransferQueueItem>> LoadAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_items);
            }

            public Task SaveAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonTransferQueueItem> items,
                CancellationToken cancellationToken = default)
            {
                SavedItems = items.ToList();
                return Task.CompletedTask;
            }

            public Task ClearAsync(Uri instanceUri, CancellationToken cancellationToken = default)
            {
                SavedItems = [];
                return Task.CompletedTask;
            }
        }

        private class FakeTransferStagingStore : ICottonTransferStagingStore
        {
            private readonly Dictionary<Guid, CottonTransferStagedFileSnapshot> _stagedFiles;

            public FakeTransferStagingStore(IReadOnlyCollection<Guid> stagedTransferIds)
            {
                _stagedFiles = stagedTransferIds.ToDictionary(
                    id => id,
                    id => new CottonTransferStagedFileSnapshot(id, $"{id:N}.bin", $"/tmp/{id:N}.bin", 1));
            }

            public IReadOnlyList<Guid> DeletedTransferIds { get; private set; } = [];

            public IReadOnlyList<IReadOnlyList<CottonTransferQueueItem>> CleanupQueueSnapshots { get; private set; } = [];

            public IReadOnlyList<Guid> StagedTransferIds => _stagedFiles.Keys.ToList();

            public Task<CottonTransferStagedFileSnapshot> StageAsync(
                Uri instanceUri,
                Guid transferId,
                string fileName,
                Stream content,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CottonTransferStagedFileSnapshot?> GetAsync(
                Uri instanceUri,
                Guid transferId,
                CancellationToken cancellationToken = default)
            {
                _stagedFiles.TryGetValue(transferId, out CottonTransferStagedFileSnapshot? stagedFile);
                return Task.FromResult(stagedFile);
            }

            public Task<IReadOnlyList<CottonTransferStagedFileSnapshot>> ListAsync(
                Uri instanceUri,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<CottonTransferStagedFileSnapshot>>(_stagedFiles.Values.ToList());
            }

            public Task DeleteAsync(Uri instanceUri, Guid transferId, CancellationToken cancellationToken = default)
            {
                _stagedFiles.Remove(transferId);
                DeletedTransferIds = DeletedTransferIds.Concat([transferId]).ToList();
                return Task.CompletedTask;
            }

            public async Task<CottonTransferStagedFileCleanupResult> CleanupAsync(
                Uri instanceUri,
                IReadOnlyCollection<CottonTransferQueueItem> queueItems,
                CancellationToken cancellationToken = default)
            {
                CleanupQueueSnapshots = CleanupQueueSnapshots.Concat([queueItems.ToList()]).ToList();
                IReadOnlySet<Guid> transferIdsToDelete =
                    CottonTransferStagedFileCleanupPolicy.ResolveTransferIdsToDelete(
                        queueItems,
                        _stagedFiles.Keys.ToList());
                long deletedBytes = transferIdsToDelete
                    .Where(transferId => _stagedFiles.ContainsKey(transferId))
                    .Sum(transferId => _stagedFiles[transferId].SizeBytes);
                int deletedFileCount = transferIdsToDelete.Count(transferId => _stagedFiles.ContainsKey(transferId));
                foreach (Guid transferId in transferIdsToDelete)
                {
                    await DeleteAsync(instanceUri, transferId, cancellationToken);
                }

                return new CottonTransferStagedFileCleanupResult(deletedFileCount, deletedBytes);
            }
        }

        private class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
            }

            public override DateTimeOffset GetUtcNow()
            {
                return _utcNow;
            }
        }
    }
}
