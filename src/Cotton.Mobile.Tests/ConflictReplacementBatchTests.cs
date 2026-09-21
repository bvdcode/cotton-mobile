// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Diagnostics;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class ConflictReplacementBatchTests
    {
        [Fact]
        public async Task InterruptedBatchKeepsOnlyRemainingFilesAndDoesNotRepeatCompletedUploads()
        {
            using RemoteConflictResolutionTestEnvironment environment = new() { FailAfterUpdates = 17 };
            environment.AddFiles(249);
            CottonSyncReviewState state = await environment.Service.ScanAsync(environment.Root, TestContext.Current.CancellationToken);
            CottonSyncVerifiedFile[] verified = [.. Enumerable.Range(0, 4000).Select(index =>
                new CottonSyncVerifiedFile($"existing:{index}", $"existing-{index}.jpg", 100, TestContentHashes.First))];
            await environment.ReviewStore.UpdateAsync(environment.Root, current => new CottonSyncReviewState(
                environment.Root.StableKey, null, 0, current.Conflicts, current.Approvals, verified),
                TestContext.Current.CancellationToken);
            Stopwatch timer = Stopwatch.StartNew();
            await environment.Service.ApproveAsync(environment.Root, state.Conflicts, TestContext.Current.CancellationToken);
            double approvalMilliseconds = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            await Assert.ThrowsAsync<HttpRequestException>(() => environment.ApplyAsync(TestContext.Current.CancellationToken));
            CottonSyncReviewState remaining = await environment.Service.LoadAsync(environment.Root, TestContext.Current.CancellationToken);
            Assert.Equal(233, remaining.Approvals.Count);
            Assert.Equal(233, remaining.Conflicts.Count);
            Assert.Equal(17, (await environment.Receipts.LoadAsync(environment.Root.InstanceUri,
                environment.Root, TestContext.Current.CancellationToken)).Count);

            environment.FailAfterUpdates = null;
            Assert.Equal(233, await environment.ApplyAsync(TestContext.Current.CancellationToken));
            Assert.Equal(250, environment.UpdateCount);
            remaining = await environment.Service.LoadAsync(environment.Root, TestContext.Current.CancellationToken);
            Assert.Empty(remaining.Approvals);
            Assert.Empty(remaining.Conflicts);
            Assert.Equal(4000, remaining.VerifiedFiles.Count);
            TestContext.Current.TestOutputHelper!.WriteLine(
                $"250 replacements with 4000 verified files: approval {approvalMilliseconds:F2} ms; processing with interrupted retry {timer.Elapsed.TotalMilliseconds:F2} ms.");
        }
    }
}
