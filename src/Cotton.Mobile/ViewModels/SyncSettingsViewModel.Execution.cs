// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public partial class SyncSettingsViewModel
    {
        private Task ExecuteRootPrimaryActionAsync(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.CanReconnect)
            {
                return ReconnectRootAsync(item);
            }

            if (item.CanRunNow)
            {
                return RunRootAsync(item);
            }

            throw new InvalidOperationException("Sync root does not have a primary action.");
        }

        private async Task RunRootAsync(CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = "Could not run sync for this instance.";
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonSyncRootRunRouting.CreateOfflineUnavailableStatus(item.Direction);
                return;
            }

            CottonSyncDirection statusDirection = item.Direction;
            IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await LoadRootCollectionAsync(instanceUri);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    ShowRoots(collection);
                    Status = "Sync folder is no longer configured.";
                    return;
                }

                statusDirection = root.Direction;
                if (collection.PausedRootIds.Contains(root.Id))
                {
                    ShowRoots(collection);
                    Status = CottonSyncRootManagementText.RootPausedStatus;
                    return;
                }

                if (!CottonSyncRootRunCapability.CanRun(root))
                {
                    ShowRoots(collection);
                    Status = "Sync folder is not ready.";
                    return;
                }

                Status = CottonSyncRootRunRouting.CreateStartingStatus(root);
                item.SetRunning(isRunning: true);
                Status = await _executionWorkflow.RunRootAsync(
                    instanceUri,
                    root,
                    status => Status = status);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile sync root.");
                Status = CottonSyncRootRunRouting.CreateFailedStatus(statusDirection);
            }
            finally
            {
                item.SetRunning(isRunning: false);
                IsBusy = false;
            }
        }

        private async Task RunAllAsync()
        {
            Uri? instanceUri = _instanceUri;
            if (instanceUri is null)
            {
                Status = "Could not run sync for this instance.";
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                Status = CottonSyncSettingsRunStatusText.OfflineUnavailableStatus;
                return;
            }

            IReadOnlyList<CottonSyncRootListItem> runningItems = [];
            IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await LoadRootCollectionAsync(instanceUri);
                IReadOnlyList<CottonSyncRootSnapshot> runnableRoots =
                    CottonSyncRootRunCapability.GetRunnableRoots(
                        collection.Roots,
                        collection.PausedRootIds);
                IReadOnlySet<Guid> runnableRootIds = runnableRoots
                    .Select(root => root.Id)
                    .ToHashSet();
                runningItems = Roots
                    .Where(item => runnableRootIds.Contains(item.Id))
                    .ToList();
                foreach (CottonSyncRootListItem runningItem in runningItems)
                {
                    runningItem.SetRunning(isRunning: true);
                }

                Status = CottonSyncSettingsRunStatusText.StartingAllStatus;
                Status = await _executionWorkflow.RunAllAsync(
                    instanceUri,
                    runnableRoots,
                    status => Status = status);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile sync roots.");
                Status = CottonSyncSettingsRunStatusText.FailedStatus;
            }
            finally
            {
                foreach (CottonSyncRootListItem runningItem in runningItems)
                {
                    runningItem.SetRunning(isRunning: false);
                }

                IsBusy = false;
            }
        }
    }
}
