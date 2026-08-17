// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Platforms.Android;
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile
{
    public partial class SyncDashboardView : ContentView
    {
        public SyncDashboardView()
        {
            InitializeComponent();
        }

        private void ShowRootActions(object? sender, EventArgs eventArgs)
        {
            Button button = sender as Button
                ?? throw new ArgumentException("Sync-root actions require a button.", nameof(sender));
            CottonSyncRootListItem item = button.BindingContext as CottonSyncRootListItem
                ?? throw new InvalidOperationException("Sync-root actions require a sync root.");
            SyncSettingsViewModel viewModel = BindingContext as SyncSettingsViewModel
                ?? throw new InvalidOperationException("Sync-root actions require sync settings.");

            AndroidSyncRootActionsMenu.Show(button, item, viewModel.RootActionCommand);
        }
    }
}
