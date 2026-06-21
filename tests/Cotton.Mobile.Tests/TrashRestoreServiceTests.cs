using Cotton.Files;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class TrashRestoreServiceTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid FileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid FolderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Fact]
        public async Task Restore_file_uses_default_request_and_maps_restored_outcome()
        {
            var client = new FakeTrashRestoreClient
            {
                FileOutcome = new RestoreOutcomeDto
                {
                    Status = RestoreStatus.Restored,
                },
            };
            var service = new CottonTrashRestoreService(client);

            CottonTrashRestoreResult result = await service.RestoreAsync(
                InstanceUri,
                FileId,
                CottonFileBrowserEntryType.File);

            Assert.Equal(FileId, result.ItemId);
            Assert.Equal(CottonFileBrowserEntryType.File, result.ItemType);
            Assert.Equal(CottonTrashRestoreRetryMode.None, result.RetryMode);
            Assert.Equal(CottonSyncRestoreOutcomeStatus.Restored, result.Status);
            Assert.True(result.IsRestored);
            Assert.True(result.IsTerminal);
            Assert.Equal(CottonTrashRestoreStatusText.RestoredStatus, result.StatusText);
            Assert.Equal(1, client.FileRestoreCallCount);
            Assert.Equal(0, client.FolderRestoreCallCount);
            Assert.Equal(InstanceUri, client.LastInstanceUri);
            Assert.Equal(FileId, client.LastItemId);
            Assert.NotNull(client.LastRequest);
            Assert.False(client.LastRequest.CreateMissingParents);
            Assert.False(client.LastRequest.Overwrite);
        }

        [Fact]
        public async Task Restore_folder_uses_node_restore_path()
        {
            var client = new FakeTrashRestoreClient
            {
                FolderOutcome = new RestoreOutcomeDto
                {
                    Status = RestoreStatus.Restored,
                },
            };
            var service = new CottonTrashRestoreService(client);

            CottonTrashRestoreResult result = await service.RestoreAsync(
                InstanceUri,
                FolderId,
                CottonFileBrowserEntryType.Folder);

            Assert.Equal(FolderId, result.ItemId);
            Assert.Equal(CottonFileBrowserEntryType.Folder, result.ItemType);
            Assert.Equal(0, client.FileRestoreCallCount);
            Assert.Equal(1, client.FolderRestoreCallCount);
            Assert.Equal(FolderId, client.LastItemId);
            Assert.Equal(CottonTrashRestoreStatusText.RestoredStatus, result.StatusText);
        }

        [Fact]
        public async Task Parent_missing_outcome_exposes_create_missing_parents_retry()
        {
            var client = new FakeTrashRestoreClient
            {
                FileOutcome = new RestoreOutcomeDto
                {
                    Status = RestoreStatus.ParentMissing,
                },
            };
            var service = new CottonTrashRestoreService(client);

            CottonTrashRestoreResult result = await service.RestoreAsync(
                InstanceUri,
                FileId,
                CottonFileBrowserEntryType.File);

            Assert.Equal(CottonSyncRestoreOutcomeStatus.ParentMissingNeedsChoice, result.Status);
            Assert.True(result.CanRetryWithCreateMissingParents);
            Assert.False(result.CanRetryWithOverwrite);
            Assert.False(result.IsTerminal);
            Assert.Equal(CottonTrashRestoreStatusText.ParentMissingStatus, result.StatusText);
        }

        [Fact]
        public async Task Conflict_outcome_exposes_overwrite_retry()
        {
            var client = new FakeTrashRestoreClient
            {
                FileOutcome = new RestoreOutcomeDto
                {
                    Status = RestoreStatus.Conflict,
                },
            };
            var service = new CottonTrashRestoreService(client);

            CottonTrashRestoreResult result = await service.RestoreAsync(
                InstanceUri,
                FileId,
                CottonFileBrowserEntryType.File);

            Assert.Equal(CottonSyncRestoreOutcomeStatus.ConflictNeedsChoice, result.Status);
            Assert.False(result.CanRetryWithCreateMissingParents);
            Assert.True(result.CanRetryWithOverwrite);
            Assert.False(result.IsTerminal);
            Assert.Equal(CottonTrashRestoreStatusText.ConflictStatus, result.StatusText);
        }

        [Fact]
        public async Task Not_restorable_outcome_is_terminal()
        {
            var client = new FakeTrashRestoreClient
            {
                FileOutcome = new RestoreOutcomeDto
                {
                    Status = RestoreStatus.NotRestorable,
                },
            };
            var service = new CottonTrashRestoreService(client);

            CottonTrashRestoreResult result = await service.RestoreAsync(
                InstanceUri,
                FileId,
                CottonFileBrowserEntryType.File);

            Assert.Equal(CottonSyncRestoreOutcomeStatus.NotRestorable, result.Status);
            Assert.False(result.IsRestored);
            Assert.True(result.IsTerminal);
            Assert.Equal(CottonTrashRestoreStatusText.NotRestorableStatus, result.StatusText);
        }

        [Theory]
        [InlineData(CottonTrashRestoreRetryMode.None, false, false)]
        [InlineData(CottonTrashRestoreRetryMode.CreateMissingParents, true, false)]
        [InlineData(CottonTrashRestoreRetryMode.Overwrite, false, true)]
        public async Task Retry_mode_controls_restore_request_flags(
            CottonTrashRestoreRetryMode retryMode,
            bool expectedCreateMissingParents,
            bool expectedOverwrite)
        {
            var client = new FakeTrashRestoreClient();
            var service = new CottonTrashRestoreService(client);

            await service.RestoreAsync(
                InstanceUri,
                FileId,
                CottonFileBrowserEntryType.File,
                retryMode);

            Assert.NotNull(client.LastRequest);
            Assert.Equal(expectedCreateMissingParents, client.LastRequest.CreateMissingParents);
            Assert.Equal(expectedOverwrite, client.LastRequest.Overwrite);
        }

        [Fact]
        public async Task Restore_rejects_invalid_inputs_before_calling_client()
        {
            var client = new FakeTrashRestoreClient();
            var service = new CottonTrashRestoreService(client);

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.RestoreAsync(
                    null!,
                    FileId,
                    CottonFileBrowserEntryType.File));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.RestoreAsync(
                    InstanceUri,
                    Guid.Empty,
                    CottonFileBrowserEntryType.File));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.RestoreAsync(
                    InstanceUri,
                    FileId,
                    (CottonFileBrowserEntryType)99));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.RestoreAsync(
                    InstanceUri,
                    FileId,
                    CottonFileBrowserEntryType.File,
                    (CottonTrashRestoreRetryMode)99));

            Assert.Equal(0, client.FileRestoreCallCount);
            Assert.Equal(0, client.FolderRestoreCallCount);
        }

        [Fact]
        public void Trash_restore_status_text_is_explicit()
        {
            Assert.Equal("Restoring notes.txt...", CottonTrashRestoreStatusText.CreateRestoringStatus(" notes.txt "));
            Assert.Equal("notes.txt restored.", CottonTrashRestoreStatusText.CreateRestoredStatus("notes.txt"));
            Assert.Equal("Item restored.", CottonTrashRestoreStatusText.CreateRestoredStatus(" "));
            Assert.Equal("Restore cancelled.", CottonTrashRestoreStatusText.CancelledStatus);
            Assert.Equal("Could not restore item.", CottonTrashRestoreStatusText.FailedStatus);
            Assert.Equal("Offline. Restore needs internet.", CottonTrashRestoreStatusText.OfflineUnavailableStatus);
        }

        private class FakeTrashRestoreClient : ICottonTrashRestoreClient
        {
            public RestoreOutcomeDto FileOutcome { get; set; } = new()
            {
                Status = RestoreStatus.Restored,
            };

            public RestoreOutcomeDto FolderOutcome { get; set; } = new()
            {
                Status = RestoreStatus.Restored,
            };

            public int FileRestoreCallCount { get; private set; }

            public int FolderRestoreCallCount { get; private set; }

            public Uri? LastInstanceUri { get; private set; }

            public Guid? LastItemId { get; private set; }

            public RestoreItemRequestDto? LastRequest { get; private set; }

            public Task<RestoreOutcomeDto> RestoreFileAsync(
                Uri instanceUri,
                Guid fileId,
                RestoreItemRequestDto request,
                CancellationToken cancellationToken = default)
            {
                FileRestoreCallCount++;
                LastInstanceUri = instanceUri;
                LastItemId = fileId;
                LastRequest = request;
                return Task.FromResult(FileOutcome);
            }

            public Task<RestoreOutcomeDto> RestoreFolderAsync(
                Uri instanceUri,
                Guid folderId,
                RestoreItemRequestDto request,
                CancellationToken cancellationToken = default)
            {
                FolderRestoreCallCount++;
                LastInstanceUri = instanceUri;
                LastItemId = folderId;
                LastRequest = request;
                return Task.FromResult(FolderOutcome);
            }
        }
    }
}
