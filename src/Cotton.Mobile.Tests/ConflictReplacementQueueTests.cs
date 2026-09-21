// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ConflictReplacementQueueTests
    {
        [Fact]
        public async Task UnselectedConflictsNeverUpload()
        {
            using RemoteConflictResolutionTestEnvironment environment = new();
            CottonSyncReviewState state = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            Assert.Single(state.Conflicts);
            Assert.Equal(0, await environment.ApplyAsync(TestContext.Current.CancellationToken));
            Assert.Equal(0, environment.UpdateCount);
        }

        [Fact]
        public async Task InterruptedRequestRetainsExactApprovalAndRetriesIt()
        {
            using RemoteConflictResolutionTestEnvironment environment = new() { FailBeforeUpdate = true };
            CottonSyncReviewState state = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            await environment.Service.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            CottonSyncReplacementApproval original = Assert.Single((await environment.ReviewStore.LoadAsync(
                environment.Root, TestContext.Current.CancellationToken)).Approvals);
            await Assert.ThrowsAsync<HttpRequestException>(() => environment.ApplyAsync(TestContext.Current.CancellationToken));
            CottonSyncReplacementApproval retained = Assert.Single((await environment.ReviewStore.LoadAsync(
                environment.Root, TestContext.Current.CancellationToken)).Approvals);
            Assert.Equal(original.OperationId, retained.OperationId);
            Assert.True(original.Conflict.Matches(retained.Conflict));

            environment.FailBeforeUpdate = false;
            Assert.Equal(1, await environment.ApplyAsync(TestContext.Current.CancellationToken));
            Assert.Empty((await environment.ReviewStore.LoadAsync(environment.Root, TestContext.Current.CancellationToken)).Approvals);
            Assert.Equal(1, environment.UpdateCount);
        }

        [Fact]
        public async Task RestartAfterServerAcceptedFileConfirmsWithoutUploadingAgain()
        {
            using RemoteConflictResolutionTestEnvironment environment = new() { InterruptAfterUpdate = true };
            CottonSyncReviewState state = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            await environment.Service.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<OperationCanceledException>(() => environment.ApplyAsync(TestContext.Current.CancellationToken));
            Assert.Single((await environment.ReviewStore.LoadAsync(environment.Root, TestContext.Current.CancellationToken)).Approvals);
            Assert.Empty(await environment.Receipts.LoadAsync(environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));

            environment.InterruptAfterUpdate = false;
            Assert.Equal(1, await environment.ApplyAsync(TestContext.Current.CancellationToken));
            Assert.Equal(1, environment.UpdateCount);
            Assert.Single(await environment.Receipts.LoadAsync(environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task CloudChangedAfterApprovalRequiresNewReviewWithoutOverwriting()
        {
            using RemoteConflictResolutionTestEnvironment environment = new();
            CottonSyncReviewState state = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            await environment.Service.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            environment.ChangeCloudRevision();
            Assert.Equal(0, await environment.ApplyAsync(TestContext.Current.CancellationToken));
            Assert.Equal(0, environment.UpdateCount);
            CottonSyncReviewState refreshed = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            Assert.Empty(refreshed.Approvals);
            Assert.Equal("changed-etag", Assert.Single(refreshed.Conflicts).RemoteETag);
        }

        [Fact]
        public async Task PhoneChangedAfterApprovalDoesNotUploadThePreviouslySelectedVersion()
        {
            using RemoteConflictResolutionTestEnvironment environment = new();
            CottonSyncReviewState state = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            await environment.Service.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            CottonDeviceToCloudLocalItemSnapshot changed = CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                "photo.jpg", "photo.jpg", DateTime.UnixEpoch, 999, "image/jpeg",
                environment.LocalFile.LocalSourceId!, TestContentHashes.Second);
            CottonDeviceToCloudRemoteContentSnapshot remote = await new CottonRecursiveRemoteContentLoader(environment)
                .LoadAsync(environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken);
            Assert.Equal(0, await environment.Service.ApplyAsync(environment.Root,
                new CottonDeviceToCloudLocalContentSnapshot("Photos", [changed]), remote, TestContext.Current.CancellationToken));
            Assert.Equal(0, environment.UpdateCount);
        }

        [Fact]
        public async Task RepeatedApprovalRetainsItsOperationIdentity()
        {
            using RemoteConflictResolutionTestEnvironment environment = new();
            CottonSyncReviewState state = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            await environment.Service.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            Guid first = Assert.Single((await environment.ReviewStore.LoadAsync(
                environment.Root, TestContext.Current.CancellationToken)).Approvals).OperationId;
            await environment.Service.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            Assert.Equal(first, Assert.Single((await environment.ReviewStore.LoadAsync(
                environment.Root, TestContext.Current.CancellationToken)).Approvals).OperationId);
        }
    }
}
