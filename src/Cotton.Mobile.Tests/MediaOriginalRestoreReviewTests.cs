// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MediaOriginalRestoreReviewTests
    {
        [Fact]
        public async Task LeavingApplicationDuringThreeHundredFileReviewStopsHashingWithoutPromptOrWork()
        {
            using MediaOriginalRestoreTestEnvironment environment = new() { WaitForHashCancellation = true };
            for (int index = 1; index < 300; index++)
            {
                string name = $"photo-{index}.jpg";
                environment.AdditionalLocalFiles.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFile(name, name,
                    DateTime.UnixEpoch, environment.OriginalBytes.Length, "image/jpeg", name, environment.LocalFile.ContentHash));
                environment.AdditionalCloudFiles.Add(environment.CreateCloudFile(environment.RedactedHash!, name));
            }

            MediaOriginalRestoreScheduler scheduler = new();
            TestApplicationForegroundService foreground = new();
            foreground.NotifyResumed();
            CottonMediaOriginalRestoreReview review = new(environment.Service, scheduler, foreground);
            int prompts = 0;
            Task checking = review.RunAsync(Collection(environment), false, (count, token) =>
            {
                prompts++;
                return Task.FromResult(true);
            }, TestContext.Current.CancellationToken);
            await environment.HashStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            foreground.NotifyStopped();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => checking.WaitAsync(
                TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(1, environment.HashCount);
            Assert.Equal(0, prompts);
            Assert.Empty(scheduler.Roots);
            Assert.Empty(environment.Transport.PublishedFiles);
            Assert.True(await environment.Service.IsReviewNeededAsync(environment.Root, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task YesPersistsAndSchedulesWithoutHashingAgainOrWaitingForRunningSync()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            MediaOriginalRestoreScheduler scheduler = new();
            TestApplicationForegroundService foreground = new();
            foreground.NotifyResumed();
            CottonMediaOriginalRestoreReview review = new(environment.Service, scheduler, foreground);
            TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task? runningSync = null;
            try
            {
                await review.RunAsync(Collection(environment), false, async (count, token) =>
                {
                    Assert.Equal(1, count);
                    Assert.Equal(1, environment.HashCount);
                    runningSync = environment.ExecutionLock.ExecuteAsync(environment.Root, async innerToken =>
                    {
                        entered.SetResult();
                        await release.Task.WaitAsync(innerToken);
                        return 0;
                    }, TestContext.Current.CancellationToken);
                    await entered.Task.WaitAsync(token);
                    foreground.NotifyStopped();
                    return true;
                }, TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

                Assert.Equal(1, environment.HashCount);
                Assert.Equal(environment.Root.Id, Assert.Single(scheduler.Roots));
                Assert.Single((await environment.RestoreStore.LoadAsync(environment.Root, TestContext.Current.CancellationToken)).Approvals);
                Assert.Empty(environment.Transport.PublishedFiles);
            }
            finally
            {
                release.TrySetResult();
                if (runningSync is not null)
                {
                    await runningSync;
                }
            }
        }

        [Fact]
        public async Task DecliningDoesNotScheduleOrRepeatPromptOnNextReview()
        {
            using MediaOriginalRestoreTestEnvironment environment = new();
            MediaOriginalRestoreScheduler scheduler = new();
            TestApplicationForegroundService foreground = new();
            foreground.NotifyResumed();
            CottonMediaOriginalRestoreReview review = new(environment.Service, scheduler, foreground);
            int prompts = 0;
            Task<bool> Confirm(int count, CancellationToken token)
            {
                prompts++;
                return Task.FromResult(false);
            }

            await review.RunAsync(Collection(environment), false, Confirm, TestContext.Current.CancellationToken);
            await review.RunAsync(Collection(environment), false, Confirm, TestContext.Current.CancellationToken);

            Assert.Equal(1, prompts);
            Assert.Empty(scheduler.Roots);
            Assert.Empty((await environment.RestoreStore.LoadAsync(environment.Root, TestContext.Current.CancellationToken)).Approvals);
        }

        private static SyncRootCollectionSnapshot Collection(MediaOriginalRestoreTestEnvironment environment)
        {
            return new SyncRootCollectionSnapshot([environment.Root], new HashSet<Guid>(),
                new Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>());
        }
    }
}
