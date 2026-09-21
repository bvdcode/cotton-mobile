// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public partial class DeviceToCloudSyncCoordinatorTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task NormalSyncAppliesApprovedRestorationBeforePlanningAndReportsConfirmedFile(
            bool savedComparison, bool conflictApproved)
        {
            MediaOriginalRestoreTestEnvironment environment = _originalRestore;
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            await environment.Service.CompleteReviewAsync(preview, true, TestContext.Current.CancellationToken);
            CottonUploadOnlySyncPlanExecutor executor = new(_fileOperator,
                new DeviceToCloudCoordinatorLocalFileOperator(), environment.Receipts, _progressHub,
                NullLogger<CottonUploadOnlySyncPlanExecutor>.Instance, TimeProvider.System);
            CottonRemoteConflictResolutionService conflicts = new(
                environment, new CottonRecursiveRemoteContentLoader(environment), environment.Receipts,
                _reviewStore, environment.Replacement, environment.ExecutionLock, _progressHub,
                TimeProvider.System, NullLogger<CottonRemoteConflictResolutionService>.Instance);
            if (conflictApproved)
            {
                CottonSyncReviewState state = await conflicts.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
                Assert.Single(state.Conflicts);
                await conflicts.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            }
            if (savedComparison)
            {
                CottonDeviceToCloudLocalContentSnapshot local = await environment.ReadAsync(
                    environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken);
                await _reviewStore.UpdateAsync(environment.Root, state => new CottonSyncReviewState(
                    environment.Root.StableKey, CottonSyncLocalFingerprint.Create(environment.Root, local), 1,
                    state.Conflicts, state.Approvals, []), TestContext.Current.CancellationToken);
            }
            CottonDeviceToCloudSyncCoordinator coordinator = new(_rootStore, _pauseStore, environment.Receipts,
                environment, new CottonRecursiveRemoteContentLoader(environment), executor, environment.ExecutionLock,
                environment.Executor, conflicts, _reviewStore, _progressHub, NullLogger<CottonDeviceToCloudSyncCoordinator>.Instance);

            CottonDeviceToCloudSyncRunSummary summary = await coordinator.RunRootAsync(
                environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken);

            CottonDeviceToCloudSyncRootRunResult result = Assert.Single(summary.RootResults);
            Assert.True(result.HasAppliedChanges);
            Assert.False(result.HasBlockedItems);
            Assert.Equal(1, result.ExecutionResult!.ConfirmedUploadCount);
            Assert.Equal(0, result.Plan!.UploadCount);
            Assert.Single(environment.Transport.PublishedFiles);
            Assert.Equal(environment.LocalFile.ContentHash, environment.CloudFile.ContentHash);
            Assert.Empty((await environment.RestoreStore.LoadAsync(environment.Root, TestContext.Current.CancellationToken)).Approvals);
        }
    }
}
