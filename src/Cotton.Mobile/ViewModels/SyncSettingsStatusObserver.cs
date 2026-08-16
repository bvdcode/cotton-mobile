// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsStatusObserver
    {
        private readonly ICottonAutomaticSyncStatusStore _statusStore;
        private readonly CottonSyncProgressHub _progressHub;
        private ISyncSettingsViewState? _state;

        public SyncSettingsStatusObserver(
            ICottonAutomaticSyncStatusStore statusStore,
            CottonSyncProgressHub progressHub)
        {
            ArgumentNullException.ThrowIfNull(statusStore);
            ArgumentNullException.ThrowIfNull(progressHub);

            _statusStore = statusStore;
            _progressHub = progressHub;
            _statusStore.StatusesChanged += OnStatusesChanged;
            _progressHub.ProgressChanged += OnProgressChanged;
        }

        public void Attach(ISyncSettingsViewState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (_state is not null && !ReferenceEquals(_state, state))
            {
                throw new InvalidOperationException("Sync status observer is already attached.");
            }

            _state = state;
        }

        public void RefreshProgress()
        {
            ISyncSettingsViewState state = _state
                ?? throw new InvalidOperationException("Sync status observer is not attached.");
            foreach (CottonSyncProgressSnapshot progress in _progressHub.GetCurrent())
            {
                ApplyProgress(
                    state,
                    new CottonSyncProgressChangedEventArgs(progress.RootId, progress));
            }
        }

        private void OnStatusesChanged(
            object? sender,
            CottonAutomaticSyncStatusesChangedEventArgs eventArgs)
        {
            ISyncSettingsViewState? state = _state;
            if (state?.InstanceUri is null || !Uri.Equals(state.InstanceUri, eventArgs.InstanceUri))
            {
                return;
            }

            HashSet<Guid> visibleRootIds = [.. state.Roots.Select(root => root.Id)];
            CottonAutomaticSyncRootStatusSnapshot[] visibleStatuses = [.. eventArgs.Statuses.Values
                .Where(status => visibleRootIds.Contains(status.RootId))];
            string? aggregateStatus = CottonAutomaticSyncStatusText.Create(visibleStatuses);
            MainThread.BeginInvokeOnMainThread(() => ApplyStatuses(
                state,
                eventArgs,
                aggregateStatus));
        }

        private static void ApplyStatuses(
            ISyncSettingsViewState state,
            CottonAutomaticSyncStatusesChangedEventArgs eventArgs,
            string? aggregateStatus)
        {
            if (!Uri.Equals(state.InstanceUri, eventArgs.InstanceUri))
            {
                return;
            }

            state.AutomaticStatus = aggregateStatus;
            foreach (CottonSyncRootListItem item in state.Roots)
            {
                eventArgs.Statuses.TryGetValue(
                    item.Id,
                    out CottonAutomaticSyncRootStatusSnapshot? status);
                item.SetAutomaticStatus(status);
            }
        }

        private void OnProgressChanged(object? sender, CottonSyncProgressChangedEventArgs eventArgs)
        {
            ISyncSettingsViewState? state = _state;
            if (state is null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => ApplyProgress(state, eventArgs));
        }

        private static void ApplyProgress(
            ISyncSettingsViewState state,
            CottonSyncProgressChangedEventArgs eventArgs)
        {
            CottonSyncRootListItem? item = state.Roots.SingleOrDefault(root => root.Id == eventArgs.RootId);
            if (item is null)
            {
                return;
            }

            CottonSyncProgressSnapshot? progress = eventArgs.Progress;
            if (progress is null)
            {
                item.CompleteProgress();
                return;
            }

            if (item.CanRunNow)
            {
                item.ApplyProgress(progress);
            }
        }
    }
}
