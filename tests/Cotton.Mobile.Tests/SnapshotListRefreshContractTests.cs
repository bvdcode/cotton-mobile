using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SnapshotListRefreshContractTests
    {
        [Fact]
        public void Sync_roots_are_replaced_with_a_single_collection_reset()
        {
            string source = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/SyncSettingsViewModel.cs");

            Assert.Contains(
                "public RangeObservableCollection<CottonSyncRootListItem> Roots { get; } = new();",
                source,
                StringComparison.Ordinal);
            Assert.Contains("Roots.ReplaceWith(state.Items);", source, StringComparison.Ordinal);
        }

        [Fact]
        public void Single_root_runtime_progress_does_not_reset_the_collection()
        {
            string source = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/SyncSettingsViewModel.Execution.cs");
            int runRootStart = source.IndexOf("private async Task RunRootAsync", StringComparison.Ordinal);
            int runAllStart = source.IndexOf("private async Task RunAllAsync", runRootStart, StringComparison.Ordinal);

            Assert.True(runRootStart >= 0);
            Assert.True(runAllStart > runRootStart);
            string runRoot = source[runRootStart..runAllStart];
            Assert.Contains("item.SetRunning(isRunning: true);", runRoot, StringComparison.Ordinal);
            Assert.Contains("item.SetRunning(isRunning: false);", runRoot, StringComparison.Ordinal);
            Assert.DoesNotContain("ShowRoots(await", runRoot, StringComparison.Ordinal);
        }

        [Fact]
        public void Run_all_runtime_progress_updates_existing_items_without_a_reset()
        {
            string source = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/SyncSettingsViewModel.Execution.cs");
            int runAllStart = source.IndexOf("private async Task RunAllAsync", StringComparison.Ordinal);

            Assert.True(runAllStart >= 0);
            string runAll = source[runAllStart..];
            Assert.Contains("runningItem.SetRunning(isRunning: true);", runAll, StringComparison.Ordinal);
            Assert.Contains("runningItem.SetRunning(isRunning: false);", runAll, StringComparison.Ordinal);
            Assert.DoesNotContain("ShowRoots(await", runAll, StringComparison.Ordinal);
        }
    }
}
