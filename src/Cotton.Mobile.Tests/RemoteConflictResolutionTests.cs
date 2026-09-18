// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class RemoteConflictResolutionTests
    {
        [Fact]
        public async Task ReplaceFileConflictsUsesExpectedRevisionAndSavesReceipt()
        {
            using RemoteConflictResolutionTestEnvironment environment = new();

            int replaced = await environment.Service.ReplaceFileConflictsAsync(
                environment.Root.InstanceUri,
                environment.Root,
                TestContext.Current.CancellationToken);

            Assert.Equal(1, replaced);
            Assert.Equal("cloud-etag-1", environment.ReceivedExpectedETag);
            Assert.Equal(environment.LocalFile.ContentHash, environment.CloudFile.ContentHash);
            CottonUploadReceiptSnapshot receipt = Assert.Single(await environment.Receipts.LoadAsync(
                environment.Root.InstanceUri,
                environment.Root,
                TestContext.Current.CancellationToken));
            Assert.True(receipt.IsUploaded);
            Assert.Equal(environment.LocalFile.ContentHash, receipt.ContentHash);
            Assert.Equal(environment.CloudFile.Id, receipt.RemoteFileId);
            Assert.Equal(environment.CloudFile.ETag, receipt.RemoteETag);
        }

        [Fact]
        public async Task ReplaceFileConflictsDoesNotReplaceFolderAtTheSamePath()
        {
            using RemoteConflictResolutionTestEnvironment environment = new();
            environment.UseFolderConflict();

            int replaced = await environment.Service.ReplaceFileConflictsAsync(
                environment.Root.InstanceUri,
                environment.Root,
                TestContext.Current.CancellationToken);

            Assert.Equal(0, replaced);
            Assert.Null(environment.ReceivedExpectedETag);
            Assert.Empty(await environment.Receipts.LoadAsync(
                environment.Root.InstanceUri,
                environment.Root,
                TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ReplaceFileConflictsConfirmsUpdateAfterResponseIsLost()
        {
            using RemoteConflictResolutionTestEnvironment environment = new()
            {
                LoseUpdateResponse = true,
            };

            int replaced = await environment.Service.ReplaceFileConflictsAsync(
                environment.Root.InstanceUri,
                environment.Root,
                TestContext.Current.CancellationToken);

            Assert.Equal(1, replaced);
            CottonUploadReceiptSnapshot receipt = Assert.Single(await environment.Receipts.LoadAsync(
                environment.Root.InstanceUri,
                environment.Root,
                TestContext.Current.CancellationToken));
            Assert.True(receipt.IsUploaded);
            Assert.Equal(environment.CloudFile.ETag, receipt.RemoteETag);
        }
    }
}
