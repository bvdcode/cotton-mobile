// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsExecutionHandler
    {
        private readonly SyncSettingsRootProvider _rootProvider;
        private readonly SyncExecutionWorkflow _executionWorkflow;
        private readonly INetworkAccessService _networkAccess;
        private readonly ILogger<SyncSettingsExecutionHandler> _logger;

        public SyncSettingsExecutionHandler(
            SyncSettingsRootProvider rootProvider,
            SyncExecutionWorkflow executionWorkflow,
            INetworkAccessService networkAccess,
            ILogger<SyncSettingsExecutionHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(rootProvider);
            ArgumentNullException.ThrowIfNull(executionWorkflow);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(logger);

            _rootProvider = rootProvider;
            _executionWorkflow = executionWorkflow;
            _networkAccess = networkAccess;
            _logger = logger;
        }

        public Task ExecutePrimaryActionAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(item);

            if (item.CanRunNow)
            {
                return RunRootAsync(state, item);
            }

            throw new InvalidOperationException("Sync root does not have a runnable primary action.");
        }

        public async Task RunAllAsync(ISyncSettingsViewState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            Uri? instanceUri = state.InstanceUri;
            if (instanceUri is null)
            {
                state.Status = AppResources.SyncRunInstanceUnavailable;
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                state.Status = CottonSyncSettingsRunStatusText.OfflineUnavailableStatus;
                return;
            }

            IReadOnlyList<CottonSyncRootListItem> runningItems = [];
            state.IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await _rootProvider.LoadAsync(state);
                IReadOnlyList<CottonSyncRootSnapshot> runnableRoots =
                    CottonSyncRootRunCapability.GetRunnableRoots(
                        collection.Roots,
                        collection.PausedRootIds);
                IReadOnlySet<Guid> runnableRootIds = runnableRoots
                    .Select(root => root.Id)
                    .ToHashSet();
                runningItems = state.Roots
                    .Where(item => runnableRootIds.Contains(item.Id))
                    .ToList();
                SetRunning(runningItems, isRunning: true);

                state.Status = CottonSyncSettingsRunStatusText.StartingAllStatus;
                state.Status = await _executionWorkflow.RunAllAsync(
                    instanceUri,
                    runnableRoots,
                    status => state.Status = status);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile sync roots.");
                state.Status = CottonSyncSettingsRunStatusText.FailedStatus;
            }
            finally
            {
                SetRunning(runningItems, isRunning: false);
                state.IsBusy = false;
            }
        }

        private async Task RunRootAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item)
        {
            Uri? instanceUri = state.InstanceUri;
            if (instanceUri is null)
            {
                state.Status = AppResources.SyncRunInstanceUnavailable;
                return;
            }

            if (!_networkAccess.HasInternetAccess)
            {
                state.Status = CottonSyncRootRunRouting.CreateOfflineUnavailableStatus(item.Direction);
                return;
            }

            CottonSyncDirection statusDirection = item.Direction;
            state.IsBusy = true;
            try
            {
                SyncRootCollectionSnapshot collection = await _rootProvider.LoadAsync(state);
                CottonSyncRootSnapshot? root = collection.Roots.FirstOrDefault(root => root.Id == item.Id);
                if (root is null)
                {
                    state.ShowRoots(collection);
                    state.Status = AppResources.SyncFolderMissing;
                    return;
                }

                statusDirection = root.Direction;
                if (collection.PausedRootIds.Contains(root.Id))
                {
                    state.ShowRoots(collection);
                    state.Status = CottonSyncRootManagementText.RootPausedStatus;
                    return;
                }

                if (!CottonSyncRootRunCapability.CanRun(root))
                {
                    state.ShowRoots(collection);
                    state.Status = AppResources.SyncFolderNotReady;
                    return;
                }

                state.Status = CottonSyncRootRunRouting.CreateStartingStatus(root);
                item.SetRunning(isRunning: true);
                state.Status = await _executionWorkflow.RunRootAsync(
                    instanceUri,
                    root,
                    status => state.Status = status);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile sync root.");
                state.Status = CottonSyncRootRunRouting.CreateFailedStatus(statusDirection);
            }
            finally
            {
                item.SetRunning(isRunning: false);
                state.IsBusy = false;
            }
        }

        private static void SetRunning(
            IReadOnlyList<CottonSyncRootListItem> items,
            bool isRunning)
        {
            foreach (CottonSyncRootListItem item in items)
            {
                item.SetRunning(isRunning);
            }
        }
    }
}
