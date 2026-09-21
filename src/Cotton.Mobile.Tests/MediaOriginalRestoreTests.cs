// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Net;
using Cotton.Mobile.Services;
using Cotton.Sdk;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MediaOriginalRestoreTests
    {
        [Fact]
        public async Task ScanOnlyOffersExactRedactedCopiesAndDoesNotMutateAnything()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);

            CottonMediaOriginalRestoreItem item = Assert.Single(preview.Items);
            Assert.Equal(environment.CloudFile.Id, item.RemoteFile.Id);
            Assert.Empty(environment.Transport.PublishedFiles);
            Assert.Empty(await environment.Receipts.LoadAsync(environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(TestContentHashes.Third)]
        public async Task UnverifiedDifferencesAreNeverOffered(string? redactedHash)
        {
            using MediaOriginalRestoreTestEnvironment environment = new() { RedactedHash = redactedHash };
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Empty(preview.Items);
            Assert.Equal(1, preview.UnverifiedCount);
            Assert.Empty(environment.Transport.PublishedFiles);
        }

        [Fact]
        public async Task MatchingOriginalsAreNotOfferedForUpload()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            environment.CloudFile = environment.CreateCloudFile(environment.LocalFile.ContentHash!);
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(
                environment.Root, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Empty(preview.Items);
            Assert.Equal(1, preview.UnchangedCount);
        }

        [Fact]
        public async Task RestoreUpdatesSameFileWithExpectedRevisionPreservesMetadataAndConfirmsReceipt()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            Guid fileId = environment.CloudFile.Id;

            int restored = await environment.RestoreAsync(preview, TestContext.Current.CancellationToken);

            Assert.Equal(1, restored);
            Assert.Equal(fileId, environment.CloudFile.Id);
            Assert.Equal($"\"{preview.Items[0].RemoteFile.ETag}\"", environment.Transport.ReceivedIfMatch);
            Assert.Equal("Keep this caption", environment.CloudFile.Metadata["caption"]);
            Assert.Equal(environment.LocalFile.ContentHash, environment.CloudFile.ContentHash);
            CottonUploadReceiptSnapshot receipt = Assert.Single(await environment.Receipts.LoadAsync(environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));
            Assert.True(receipt.IsUploaded);
            Assert.Equal(fileId, receipt.RemoteFileId);
            Assert.Null(Assert.Single(environment.Transport.PublishedFiles).Metadata);
            CottonMediaOriginalRestorePreview repeated = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Empty(repeated.Items);
            Assert.Equal(1, repeated.UnchangedCount);
        }

        [Fact]
        public async Task ChangedCloudFileIsRejectedBeforeAnyUpload()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            environment.CloudFile = environment.CreateCloudFile(TestContentHashes.Third, fileId: environment.CloudFile.Id);

            Assert.Equal(0, await environment.RestoreAsync(preview, TestContext.Current.CancellationToken));

            Assert.Empty(environment.Transport.Chunks);
            Assert.Empty(environment.Transport.PublishedFiles);
            Assert.Empty(await environment.Receipts.LoadAsync(environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ChangedLocalBytesNeverReplaceCloudContent()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            environment.OriginalBytes[0] ^= 1;

            await Assert.ThrowsAsync<InvalidDataException>(() => environment.RestoreAsync(preview, TestContext.Current.CancellationToken));

            Assert.Empty(environment.Transport.PublishedFiles);
            Assert.Equal(preview.Items[0].RemoteFile.ContentHash, environment.CloudFile.ContentHash);
        }

        [Fact]
        public async Task ServerRevisionConflictKeepsCloudContentAndRequiresANewPreview()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            environment.Transport.UpdateFailureStatus = HttpStatusCode.PreconditionFailed;

            CottonApiException failure = await Assert.ThrowsAsync<CottonApiException>(() => environment.RestoreAsync(preview, TestContext.Current.CancellationToken));

            Assert.Equal(HttpStatusCode.PreconditionFailed, failure.StatusCode);
            Assert.Empty(environment.Transport.PublishedFiles);
            Assert.Equal(preview.Items[0].RemoteFile.ContentHash, environment.CloudFile.ContentHash);
        }

        [Fact]
        public async Task LostResponseIsConfirmedByNormalSyncWithoutAnotherUpload()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            environment.Transport.LoseUpdateResponse = true;

            Assert.Equal(1, await environment.RestoreAsync(preview, TestContext.Current.CancellationToken));
            Assert.Equal(0, await environment.ExecuteAsync(TestContext.Current.CancellationToken));
            CottonUploadReceiptSnapshot receipt = Assert.Single(await environment.Receipts.LoadAsync(
                environment.Root.InstanceUri, environment.Root, TestContext.Current.CancellationToken));
            Assert.True(receipt.IsUploaded);
            Assert.Empty((await environment.RestoreStore.LoadAsync(environment.Root, TestContext.Current.CancellationToken)).Approvals);
            Assert.Single(environment.Transport.PublishedFiles);
        }

        [Fact]
        public async Task CancelledOrUnselectedRestoreDoesNotChangeCloudFiles()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            Assert.False(await environment.Service.CompleteReviewAsync(preview, false, TestContext.Current.CancellationToken));
            Assert.Equal(0, await environment.ExecuteAsync(TestContext.Current.CancellationToken));
            Assert.True(await environment.Service.CompleteReviewAsync(preview, true, TestContext.Current.CancellationToken));
            using CancellationTokenSource cancellation = new();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => environment.ExecuteAsync(cancellation.Token));
            Assert.Single((await environment.RestoreStore.LoadAsync(environment.Root, TestContext.Current.CancellationToken)).Approvals);

            Assert.Empty(environment.Transport.PublishedFiles);
            Assert.Empty(environment.Transport.Chunks);
        }

        [Fact]
        public async Task LostPermissionPreventsScanAndRestore()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            CottonMediaOriginalRestorePreview preview = await environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken);
            environment.CanReadOriginals = false;

            await Assert.ThrowsAsync<InvalidOperationException>(() => environment.Service.ScanAsync(environment.Root, cancellationToken: TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => environment.RestoreAsync(preview, TestContext.Current.CancellationToken));
            Assert.Empty(environment.Transport.PublishedFiles);
        }
    }
}
