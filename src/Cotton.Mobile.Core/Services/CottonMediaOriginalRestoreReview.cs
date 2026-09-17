// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMediaOriginalRestoreReview(
        CottonMediaOriginalRestoreService restoreService,
        ICottonAutomaticSyncBackgroundScheduler backgroundScheduler,
        IApplicationForegroundService foreground)
    {
        public async Task RunAsync(
            SyncRootCollectionSnapshot collection,
            bool reviewCompletedRoots,
            Func<int, CancellationToken, Task<bool>> confirm,
            CancellationToken sessionCancellation)
        {
            if (!restoreService.IsSupported || !restoreService.CanReadOriginals)
            {
                return;
            }

            await using CottonForegroundCancellation lifetime = new(foreground, sessionCancellation);
            CancellationToken reviewCancellation = lifetime.Token;
            List<CottonMediaOriginalRestorePreview> previews = [];
            foreach (CottonSyncRootSnapshot root in collection.Roots)
            {
                reviewCancellation.ThrowIfCancellationRequested();
                if (collection.PausedRootIds.Contains(root.Id) || !CottonDeviceToCloudSyncRootCapability.CanRun(root)
                    || (!reviewCompletedRoots && !await restoreService.IsReviewNeededAsync(root, reviewCancellation).ConfigureAwait(false)))
                {
                    continue;
                }

                CottonMediaOriginalRestorePreview preview = await Task.Run(
                    () => restoreService.ScanAsync(root, cancellationToken: reviewCancellation), reviewCancellation).ConfigureAwait(false);
                previews.Add(preview);
            }

            reviewCancellation.ThrowIfCancellationRequested();
            int count = previews.Sum(preview => preview.Items.Count);
            bool restore = count > 0 && await confirm(count, reviewCancellation).ConfigureAwait(false);
            List<Guid> rootsToRun = [];
            foreach (CottonMediaOriginalRestorePreview preview in previews)
            {
                if (await restoreService.CompleteReviewAsync(preview, restore, sessionCancellation).ConfigureAwait(false))
                {
                    rootsToRun.Add(preview.Root.Id);
                }
            }

            if (rootsToRun.Count > 0)
            {
                await backgroundScheduler.ScheduleRootRetriesAsync(rootsToRun, sessionCancellation).ConfigureAwait(false);
            }
        }
    }
}
