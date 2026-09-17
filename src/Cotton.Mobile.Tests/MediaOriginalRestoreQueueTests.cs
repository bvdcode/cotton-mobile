// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MediaOriginalRestoreQueueTests
    {
        [Fact]
        public async Task MetadataEditedAfterReviewIsPreservedByContentOnlyUpdate()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            environment.Transport.ExistingMetadata["caption"] = "Edited after review";

            Assert.Equal(1, await environment.RestoreAsync(preview, TestContext.Current.CancellationToken));

            Assert.Equal("Edited after review", environment.CloudFile.Metadata["caption"]);
            Assert.Null(Assert.Single(environment.Transport.PublishedFiles).Metadata);
        }

        [Fact]
        public async Task InterruptedUploadKeepsApprovalAndContinuesWithoutPublishingTwice()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            await environment.Service.CompleteReviewAsync(preview, true, TestContext.Current.CancellationToken);
            using CancellationTokenSource stopping = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            environment.Transport.CancelAfterChunk = stopping;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => environment.ExecuteAsync(stopping.Token));

            Assert.Empty(environment.Transport.PublishedFiles);
            CottonMediaOriginalRestoreApproval pending = Assert.Single((await environment.RestoreStore.LoadAsync(
                environment.Root, TestContext.Current.CancellationToken)).Approvals);
            environment.Transport.CancelAfterChunk = null;
            Assert.Equal(1, await environment.ExecuteAsync(TestContext.Current.CancellationToken));
            Assert.Single(environment.Transport.PublishedFiles);
            Assert.Single(environment.Transport.UploadedHashes);
            CottonUploadReceiptSnapshot receipt = Assert.Single(await environment.Receipts.LoadAsync(
                environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));
            Assert.Equal(pending.OperationId, receipt.OperationId);
            Assert.Equal(0, await environment.ExecuteAsync(TestContext.Current.CancellationToken));
            Assert.Single(environment.Transport.PublishedFiles);
        }

        [Fact]
        public async Task VerifiedRestorationReplacesOldUploadedReceiptWithoutRelaxingNormalReceiptRules()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonDeviceToCloudLocalItemSnapshot file = environment.LocalFile;
            Guid previousOperation = Guid.NewGuid();
            CottonUploadReceiptSnapshot pending = new(file.LocalSourceId!, file.RelativePath, file.LocalUpdatedAtUtc,
                file.SizeBytes, file.ContentType, previousOperation, CottonUploadReceiptStatus.Pending,
                DateTime.UnixEpoch, null, null, environment.RedactedHash);
            CottonUploadReceiptSnapshot uploaded = new(file.LocalSourceId!, file.RelativePath, file.LocalUpdatedAtUtc,
                file.SizeBytes, file.ContentType, previousOperation, CottonUploadReceiptStatus.Uploaded,
                DateTime.UnixEpoch, environment.CloudFile.Id, environment.CloudFile.ETag, environment.RedactedHash);
            await environment.Receipts.SaveAsync(environment.Root.InstanceUri, environment.Root, pending, TestContext.Current.CancellationToken);
            await environment.Receipts.SaveAsync(environment.Root.InstanceUri, environment.Root, uploaded, TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<InvalidDataException>(() => environment.Receipts.SaveAsync(
                environment.Root.InstanceUri, environment.Root, pending, TestContext.Current.CancellationToken));

            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(1, await environment.RestoreAsync(preview, TestContext.Current.CancellationToken));

            CottonUploadReceiptSnapshot restored = Assert.Single(await environment.Receipts.LoadAsync(
                environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));
            Assert.True(restored.IsUploaded);
            Assert.Equal(file.ContentHash, restored.ContentHash);
            Assert.NotEqual(previousOperation, restored.OperationId);
            Assert.Equal(uploaded.RemoteFileId, restored.RemoteFileId);
        }

        [Fact]
        public async Task RepeatedConsentKeepsOneOperationAndMissingPermissionPreservesQueue()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            await environment.Service.CompleteReviewAsync(preview, true, TestContext.Current.CancellationToken);
            Guid operation = Assert.Single((await environment.RestoreStore.LoadAsync(
                environment.Root, TestContext.Current.CancellationToken)).Approvals).OperationId;
            await environment.Service.CompleteReviewAsync(preview, true, TestContext.Current.CancellationToken);
            environment.CanReadOriginals = false;

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => environment.ExecuteAsync(TestContext.Current.CancellationToken));

            Assert.Equal(operation, Assert.Single((await environment.RestoreStore.LoadAsync(
                environment.Root, TestContext.Current.CancellationToken)).Approvals).OperationId);
            Assert.Empty(environment.Transport.Chunks);
            Assert.Empty(environment.Transport.PublishedFiles);
        }
    }
}
