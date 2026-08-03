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
    }
}
