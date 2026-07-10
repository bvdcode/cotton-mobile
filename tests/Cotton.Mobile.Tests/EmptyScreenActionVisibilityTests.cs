using Xunit;

namespace Cotton.Mobile.Tests
{
    public class EmptyScreenActionVisibilityTests
    {
        [Fact]
        public void Empty_data_screens_bind_irrelevant_actions_to_content_state()
        {
            string transfersPage = RepositoryPath.ReadText("src/Cotton.Mobile/TransfersPage.xaml");
            string capturePage = RepositoryPath.ReadText("src/Cotton.Mobile/CaptureInboxPage.xaml");
            string recentPage = RepositoryPath.ReadText("src/Cotton.Mobile/RecentFilesPage.xaml");
            string trashPage = RepositoryPath.ReadText("src/Cotton.Mobile/TrashPage.xaml");

            Assert.Contains("IsPrimaryActionVisible=\"{Binding IsRunWaitingActionVisible}\"", transfersPage);
            Assert.Contains("IsSecondaryActionVisible=\"{Binding IsClearHistoryActionVisible}\"", transfersPage);
            Assert.Contains("IsPrimaryActionVisible=\"{Binding IsClearActionVisible}\"", capturePage);
            Assert.Contains("IsPrimaryActionVisible=\"{Binding IsClearActionVisible}\"", recentPage);
            Assert.Contains("IsPrimaryActionVisible=\"{Binding IsEmptyTrashActionVisible}\"", trashPage);
            Assert.Contains("IsSecondaryActionVisible=\"{Binding IsSelectionActionVisible}\"", trashPage);
            Assert.Contains("IsClusterVisible=\"{Binding IsHeaderActionClusterVisible}\"", trashPage);
        }

        [Fact]
        public void Action_availability_tracks_the_underlying_source_items()
        {
            string transfers = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/TransfersViewModel.cs");
            string capture = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/CaptureInboxViewModel.cs");
            string recent = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/RecentFilesViewModel.cs");
            string trash = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/TrashViewModel.cs");

            Assert.Contains("() => !IsBusy && _canRunWaiting", transfers);
            Assert.Contains("item.Status == CottonTransferStatus.Queued", transfers);
            Assert.Contains("public bool IsClearActionVisible => Items.Count > 0;", capture);
            Assert.Contains("public bool IsClearActionVisible => Items.Count > 0;", recent);
            Assert.Contains("public bool IsEmptyTrashActionVisible => _allItems.Count > 0;", trash);
            Assert.Contains("public bool IsSelectionActionVisible => _allItems.Count > 0;", trash);
            Assert.Contains("public bool IsHeaderActionClusterVisible => _allItems.Count > 0 || IsSearchVisible;", trash);
            Assert.Contains("() => !IsBusy && (_allItems.Count > 0 || IsSearchVisible)", trash);
        }
    }
}
