using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MinimalAppPresentationTests
    {
        [Fact]
        public void Main_page_preserves_sign_in_and_exposes_only_sync_and_profile()
        {
            XDocument page = ReadXaml("src/Cotton.Mobile/MainPage.xaml");
            IReadOnlyList<string> elements = page
                .Descendants()
                .Select(element => element.Name.LocalName)
                .ToList();

            Assert.Contains("BrandHeaderView", elements);
            Assert.Contains("AuthSignInPanelView", elements);
            Assert.Single(elements, name => name == "SyncDashboardView");
            Assert.Single(elements, name => name == "ProfileView");
            Assert.Single(elements, name => name == "AppNavigationBarView");
            Assert.DoesNotContain("FileBrowserNavigationBarView", elements);
            Assert.DoesNotContain("FileBrowserContentGridView", elements);
        }

        [Fact]
        public void App_navigation_bar_has_exactly_two_destinations()
        {
            string navigation = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Controls/AppNavigationBarView.cs");

            Assert.Contains("ColumnCount = 2", navigation, StringComparison.Ordinal);
            Assert.Contains("CreateItem(\"Sync\"", navigation, StringComparison.Ordinal);
            Assert.Contains("CreateItem(\"Profile\"", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("Files", navigation, StringComparison.Ordinal);
            Assert.DoesNotContain("Backup", navigation, StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_dashboard_uses_explicit_empty_and_list_states()
        {
            XDocument page = ReadXaml("src/Cotton.Mobile/SyncDashboardView.xaml");
            IReadOnlyList<string> elements = page
                .Descendants()
                .Select(element => element.Name.LocalName)
                .ToList();

            Assert.Single(elements, name => name == "EmptyStateView");
            Assert.Single(elements, name => name == "MaterialCollectionView");
            Assert.Single(elements, name => name == "ScreenStatusView");
            Assert.Contains("IsBackActionVisible=\"False\"", page.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void Profile_surface_keeps_account_privacy_and_sign_out_together()
        {
            string profile = RepositoryPath.ReadText("src/Cotton.Mobile/ProfileView.xaml");

            Assert.Contains("Title=\"Account\"", profile, StringComparison.Ordinal);
            Assert.Contains("Text=\"Privacy policy\"", profile, StringComparison.Ordinal);
            Assert.Contains("Text=\"Sign out\"", profile, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding LogoutCommand}\"", profile, StringComparison.Ordinal);
        }

        [Fact]
        public void Root_top_bars_can_hide_back_navigation()
        {
            string topAppBar = RepositoryPath.ReadText("src/Cotton.Mobile/Controls/TopAppBar.xaml");

            Assert.Contains("Path=IsBackActionVisible", topAppBar, StringComparison.Ordinal);
        }

        private static XDocument ReadXaml(string relativePath)
        {
            return XDocument.Parse(RepositoryPath.ReadText(relativePath));
        }
    }
}
