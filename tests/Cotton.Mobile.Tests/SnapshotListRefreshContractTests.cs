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
