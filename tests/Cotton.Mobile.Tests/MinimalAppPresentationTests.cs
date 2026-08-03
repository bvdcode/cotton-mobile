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
            Assert.Contains("ActionCommand=\"{Binding AddRootCommand}\"", page.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_setup_selects_cloud_and_device_folders_for_bidirectional_sync()
        {
            XDocument picker = ReadXaml("src/Cotton.Mobile/CloudFolderPickerPage.xaml");
            string coordinator = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Services/SyncRootSetupCoordinator.cs");

            Assert.Single(picker.Descendants(), element => element.Name.LocalName == "MaterialCollectionView");
            Assert.Contains("PrimaryCommand=\"{Binding ChooseCommand}\"", picker.ToString(), StringComparison.Ordinal);
            Assert.Contains("_cloudFolderPicker", coordinator, StringComparison.Ordinal);
            Assert.Contains(".PickAsync(instanceUri", coordinator, StringComparison.Ordinal);
            Assert.Contains("_localRootPicker", coordinator, StringComparison.Ordinal);
            Assert.Contains("EnableUserSelectedDocumentTreeRootAsync", coordinator, StringComparison.Ordinal);
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
            string topAppBarCode = RepositoryPath.ReadText("src/Cotton.Mobile/Controls/TopAppBar.xaml.cs");

            Assert.Contains("Path=IsBackActionVisible", topAppBar, StringComparison.Ordinal);
            Assert.Contains("BackCommandProperty", topAppBarCode, StringComparison.Ordinal);
        }

        [Fact]
        public void App_loads_theme_resources_before_creating_the_shell()
        {
            string app = RepositoryPath.ReadText("src/Cotton.Mobile/App.xaml.cs");

            int resourcesLoaded = app.IndexOf("InitializeComponent();", StringComparison.Ordinal);
            int shellCreated = app.IndexOf("_appShellFactory()", StringComparison.Ordinal);

            Assert.True(resourcesLoaded >= 0);
            Assert.True(shellCreated > resourcesLoaded);
            Assert.Contains("Func<AppShell>", app, StringComparison.Ordinal);
        }

        private static XDocument ReadXaml(string relativePath)
        {
            return XDocument.Parse(RepositoryPath.ReadText(relativePath));
        }
    }
}
