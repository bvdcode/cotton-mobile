// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using EasyExtensions.Mediator;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsExecutionHandler
    {
        private readonly SyncSettingsRootProvider _rootProvider;
        private readonly IMediator _mediator;
        private readonly INetworkAccessService _networkAccess;
        private readonly ILogger<SyncSettingsExecutionHandler> _logger;

        public SyncSettingsExecutionHandler(
            SyncSettingsRootProvider rootProvider,
            IMediator mediator,
            INetworkAccessService networkAccess,
            ILogger<SyncSettingsExecutionHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(rootProvider);
            ArgumentNullException.ThrowIfNull(mediator);
            ArgumentNullException.ThrowIfNull(networkAccess);
            ArgumentNullException.ThrowIfNull(logger);

            _rootProvider = rootProvider;
            _mediator = mediator;
            _networkAccess = networkAccess;
            _logger = logger;
        }

        public Task ExecutePrimaryActionAsync(
            ISyncSettingsViewState state,
            CottonSyncRootListItem item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(item);

            if (item.CanRunNow)
            {
                return RunRootAsync(state, item, cancellationToken);
            }

            throw new InvalidOperationException("Sync root does not have a runnable primary action.");
        }

        public async Task RunAllAsync(
            ISyncSettingsViewState state,
            CancellationToken cancellationToken = default)
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
                SyncRootCollectionSnapshot collection = await _rootProvider.LoadAsync(state, cancellationToken);
                IReadOnlyList<CottonSyncRootSnapshot> runnableRoots =
                    CottonSyncRootRunCapability.GetRunnableRoots(
                        collection.Roots,
                        collection.PausedRootIds);
                HashSet<Guid> runnableRootIds = [.. runnableRoots.Select(root => root.Id)];
                runningItems = [.. state.Roots.Where(item => runnableRootIds.Contains(item.Id))];
                SetRunning(runningItems, isRunning: true);

                state.Status = CottonSyncSettingsRunStatusText.StartingAllStatus;
                state.Status = await _mediator.Send(
                    new RunAllSyncRootsRequest(
                        instanceUri,
                        runnableRoots,
                        status => state.Status = status),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to run Cotton mobile sync roots.", exception);
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
            CottonSyncRootListItem item,
            CancellationToken cancellationToken)
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
                SyncRootCollectionSnapshot collection = await _rootProvider.LoadAsync(state, cancellationToken);
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
                state.Status = await _mediator.Send(
                    new RunSyncRootRequest(
                        instanceUri,
                        root,
                        status => state.Status = status),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                state.Status = null;
                throw;
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to run Cotton mobile sync root.", exception);
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
