using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SnapshotListRefreshContractTests
    {
        public static TheoryData<string, string, string> SnapshotListContracts => new()
        {
            {
                "src/Cotton.Mobile/ViewModels/RecentFilesViewModel.cs",
                "public RangeObservableCollection<CottonRecentFileListItem> Items { get; } = [];",
                "Items.ReplaceWith(snapshot.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/TransfersViewModel.cs",
                "public RangeObservableCollection<CottonTransferListItem> Items { get; }",
                "Items.ReplaceWith(snapshot.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/CaptureInboxViewModel.cs",
                "public RangeObservableCollection<CottonCaptureInboxListItem> Items { get; }",
                "Items.ReplaceWith(snapshot.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/FileVersionHistoryViewModel.cs",
                "public RangeObservableCollection<CottonFileVersionItemSnapshot> Items { get; } = [];",
                "Items.ReplaceWith(snapshot.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/CaptureDestinationPickerViewModel.cs",
                "public RangeObservableCollection<CaptureDestinationFolderItemViewModel> Folders { get; }",
                "Folders.ReplaceWith("
            },
            {
                "src/Cotton.Mobile/ViewModels/SyncSettingsViewModel.cs",
                "public RangeObservableCollection<CottonSyncRootListItem> Roots { get; } = new();",
                "Roots.ReplaceWith(state.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/TrashViewModel.cs",
                "public RangeObservableCollection<CottonFileBrowserEntry> Items { get; } = [];",
                "Items.ReplaceWith(state.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/DiagnosticsViewModel.cs",
                "public RangeObservableCollection<DiagnosticsSectionViewModel> Sections { get; } = new();",
                "Sections.ReplaceWith(CreateSections(summary, remotePush));"
            },
            {
                "src/Cotton.Mobile/ViewModels/NotificationSettingsViewModel.cs",
                "public RangeObservableCollection<RemotePushPreferenceItemViewModel> RemotePushPreferences { get; } = [];",
                "RemotePushPreferences.ReplaceWith(display.Items.Select(CreateRemotePushPreferenceViewModel));"
            },
            {
                "src/Cotton.Mobile/ViewModels/StorageSettingsViewModel.cs",
                "public RangeObservableCollection<CottonOnDeviceStorageBucketSnapshot> OnDeviceBuckets { get; } = new();",
                "OnDeviceBuckets.ReplaceWith(summary.Buckets);"
            },
            {
                "src/Cotton.Mobile/ViewModels/StorageSettingsViewModel.cs",
                "public RangeObservableCollection<CottonStorageBudgetBucketSnapshot> StorageBudgetBuckets { get; } = new();",
                "StorageBudgetBuckets.ReplaceWith(budget.Buckets);"
            },
            {
                "src/Cotton.Mobile/ViewModels/SecuritySettingsViewModel.cs",
                "public RangeObservableCollection<CottonAccountSessionListItem> AccountSessions { get; } = [];",
                "AccountSessions.ReplaceWith(display.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/SecuritySettingsViewModel.cs",
                "public RangeObservableCollection<CottonPermissionLedgerItem> PermissionLedgerItems { get; } = [];",
                "PermissionLedgerItems.ReplaceWith(display.Items);"
            },
            {
                "src/Cotton.Mobile/ViewModels/PdfViewerViewModel.cs",
                "public RangeObservableCollection<PdfPreviewPageSnapshot> Pages { get; }",
                "Pages.ReplaceWith(document.Pages);"
            },
            {
                "src/Cotton.Mobile/ViewModels/ActivityFeedViewModel.cs",
                "public RangeObservableCollection<CottonActivityFeedListItem> Items { get; } = [];",
                "Items.ReplaceWith(snapshot.Items);"
            },
        };

        [Theory]
        [MemberData(nameof(SnapshotListContracts))]
        public void Snapshot_list_view_models_replace_visible_items_with_single_reset(
            string sourcePath,
            string collectionDeclaration,
            string replacementCall)
        {
            string source = RepositoryPath.ReadText(sourcePath);

            Assert.Contains(collectionDeclaration, source, StringComparison.Ordinal);
            Assert.Contains(replacementCall, source, StringComparison.Ordinal);
        }
    }
}
