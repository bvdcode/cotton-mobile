// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public interface ISyncSettingsViewState
    {
        Uri? InstanceUri { get; }

        string? AccountScopeKey { get; }

        bool IsBusy { get; set; }

        string? Status { get; set; }

        long StatusRevision { get; }

        string? AutomaticStatus { get; set; }

        IReadOnlyList<CottonSyncRootListItem> Roots { get; }

        void ShowRoots(SyncRootCollectionSnapshot collection);
    }
}
