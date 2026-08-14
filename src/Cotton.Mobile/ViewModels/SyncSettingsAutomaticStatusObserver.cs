// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.ViewModels
{
    public class SyncSettingsAutomaticStatusObserver
    {
        private readonly ICottonAutomaticSyncStatusStore _statusStore;
        private ISyncSettingsViewState? _state;

        public SyncSettingsAutomaticStatusObserver(ICottonAutomaticSyncStatusStore statusStore)
        {
            ArgumentNullException.ThrowIfNull(statusStore);

            _statusStore = statusStore;
            _statusStore.StatusesChanged += OnStatusesChanged;
        }

        public void Attach(ISyncSettingsViewState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (_state is not null && !ReferenceEquals(_state, state))
            {
                throw new InvalidOperationException("Automatic sync status observer is already attached.");
            }

            _state = state;
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
            string? statusText = CottonAutomaticSyncStatusText.Create(visibleStatuses);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Uri.Equals(state.InstanceUri, eventArgs.InstanceUri))
                {
                    state.AutomaticStatus = statusText;
                }
            });
        }
    }
}
