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
        public void Auth_legal_footer_uses_its_own_layout_row()
        {
            XDocument page = ReadXaml("src/Cotton.Mobile/MainPage.xaml");
            XElement footer = page
                .Descendants()
                .Single(element => element.Name.LocalName == "AuthLegalFooterView");
            XAttribute row = footer
                .Attributes()
                .Single(attribute => attribute.Name.LocalName == "Grid.Row");

            Assert.Equal("1", row.Value);
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
        public void App_navigation_selection_style_updates_without_transition_frames()
        {
            string navigation = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Controls/AppNavigationBarView.cs");
            string item = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Controls/NavigationBarItem.cs");

            Assert.Contains("item.IsSelected = isSelected;", navigation, StringComparison.Ordinal);
            Assert.Contains("UpdateVisualState(false);", item, StringComparison.Ordinal);
            Assert.Contains("_isApplyingSelection", item, StringComparison.Ordinal);
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
            Assert.DoesNotContain("ScreenStatusView", elements);
            Assert.Contains(
                "SupportingText=\"{Binding HeaderSupportingText}\"",
                page.ToString(),
                StringComparison.Ordinal);
            Assert.Contains("IsBackActionVisible=\"False\"", page.ToString(), StringComparison.Ordinal);
            Assert.Contains("ActionCommand=\"{Binding AddRootCommand}\"", page.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void Empty_state_supports_short_viewports()
        {
            string emptyState = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Controls/EmptyStateView.cs");

            Assert.Contains("UpdateLayout(width > height);", emptyState, StringComparison.Ordinal);
            Assert.Contains("M3EmptyStateCompactLayout", emptyState, StringComparison.Ordinal);
            Assert.Contains("M3EmptyStateCompactSurface", emptyState, StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_setup_chooses_mode_before_cloud_and_device_folders()
        {
            XDocument optionsPage = ReadXaml("src/Cotton.Mobile/SyncRootSetupOptionsPage.xaml");
            XDocument picker = ReadXaml("src/Cotton.Mobile/CloudFolderPickerPage.xaml");
            string coordinator = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Services/SyncRootSetupCoordinator.cs");

            Assert.Equal(
                2,
                optionsPage.Descendants().Count(element => element.Name.LocalName == "SyncRootModeOptionView"));
            Assert.Single(optionsPage.Descendants(), element => element.Name.LocalName == "Switch");
            Assert.Contains("IsVisible=\"{Binding IsDeleteOptionVisible}\"", optionsPage.ToString(), StringComparison.Ordinal);
            Assert.Single(picker.Descendants(), element => element.Name.LocalName == "MaterialCollectionView");
            Assert.Contains("PrimaryCommand=\"{Binding ChooseCommand}\"", picker.ToString(), StringComparison.Ordinal);
            Assert.Contains("_optionsPicker", coordinator, StringComparison.Ordinal);
            Assert.Contains(".PickAsync(cancellationToken)", coordinator, StringComparison.Ordinal);
            Assert.Contains("_cloudFolderPicker", coordinator, StringComparison.Ordinal);
            Assert.Contains(".PickAsync(instanceUri", coordinator, StringComparison.Ordinal);
            Assert.Contains("_localRootPicker", coordinator, StringComparison.Ordinal);
            Assert.Contains("ConfigureUserSelectedDocumentTreeRootAsync", coordinator, StringComparison.Ordinal);
            Assert.Contains("options.UploadOriginalRetention", coordinator, StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_settings_delegates_storage_and_execution_workflows()
        {
            string viewModel = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/SyncSettingsViewModel.cs");

            Assert.Contains("SyncRootManager", viewModel, StringComparison.Ordinal);
            Assert.Contains("SyncExecutionWorkflow", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain("private readonly ICottonSyncRootStore", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "private readonly CottonCloudToDeviceSyncCoordinator",
                viewModel,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "private readonly CottonDeviceToCloudSyncCoordinator",
                viewModel,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "private readonly CottonBidirectionalSyncCoordinator",
                viewModel,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_root_primary_action_switches_between_run_and_local_folder_reconnect()
        {
            XDocument page = ReadXaml("src/Cotton.Mobile/SyncDashboardView.xaml");
            string coordinator = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Services/SyncRootSetupCoordinator.cs");
            string viewModel = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/SyncSettingsViewModel.cs");

            Assert.Contains("RootPrimaryActionCommand", page.ToString(), StringComparison.Ordinal);
            Assert.Contains("Binding=\"{Binding CanReconnect}\"", page.ToString(), StringComparison.Ordinal);
            Assert.Contains("ReconnectLocalRootAsync", coordinator, StringComparison.Ordinal);
            Assert.Contains("ReconnectRootAsync", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain("RunRootCommand", page.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void Profile_surface_keeps_account_privacy_and_sign_out_together()
        {
            string profile = RepositoryPath.ReadText("src/Cotton.Mobile/ProfileView.xaml");

            Assert.Contains("Title=\"Account\"", profile, StringComparison.Ordinal);
            Assert.Contains("Text=\"Privacy policy\"", profile, StringComparison.Ordinal);
            Assert.Contains("Text=\"Sign out\"", profile, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding LogoutCommand}\"", profile, StringComparison.Ordinal);
            Assert.Contains("IsSupportingTextMultiline=\"True\"", profile, StringComparison.Ordinal);
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
        public void Outlined_input_visibility_updates_atomically()
        {
            string input = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Controls/OutlinedInputField.cs");

            Assert.Contains("IsVisible = IsFieldVisible;", input, StringComparison.Ordinal);
            Assert.Contains("Opacity = 1d;", input, StringComparison.Ordinal);
            Assert.DoesNotContain("FieldVisibilityAnimationName", input, StringComparison.Ordinal);
        }

        [Fact]
        public void Authorization_cancel_wins_over_a_late_service_result()
        {
            string viewModel = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");

            int resultReceived = viewModel.IndexOf(
                "CottonSessionResult result = await _sessionService.SignInWithBrowserAsync",
                StringComparison.Ordinal);
            int cancellationChecked = viewModel.IndexOf(
                "if (authorizationCancellation.IsCancellationRequested)",
                resultReceived,
                StringComparison.Ordinal);
            int resultApplied = viewModel.IndexOf(
                "await ApplySessionResultAsync(result, ReadyStatus);",
                resultReceived,
                StringComparison.Ordinal);

            Assert.True(resultReceived >= 0);
            Assert.True(cancellationChecked > resultReceived);
            Assert.True(resultApplied > cancellationChecked);
            Assert.Contains(
                "catch (Exception exception) when (authorizationCancellation.IsCancellationRequested)",
                viewModel,
                StringComparison.Ordinal);
            Assert.Contains("Display.InstanceUrl = signInInstanceUrl;", viewModel, StringComparison.Ordinal);
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

        [Fact]
        public void Brand_mark_uses_a_dark_backplate_without_recoloring_the_artwork()
        {
            string mark = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Resources/AppIcon/cotton_brand_mark.svg");
            string splashMark = RepositoryPath.ReadText(
                "src/Cotton.Mobile/Platforms/Android/Resources/drawable/cotton_splash_mark.xml");
            string styles = RepositoryPath.ReadText("src/Cotton.Mobile/Resources/Styles/Styles.xaml");

            Assert.Contains("fill:#C6FF00", mark, StringComparison.Ordinal);
            Assert.Contains("android:shape=\"oval\"", splashMark, StringComparison.Ordinal);
            Assert.Contains("@color/cotton_brand_surface", splashMark, StringComparison.Ordinal);
            Assert.Contains("{StaticResource M3BrandSurface}", styles, StringComparison.Ordinal);
        }

        private static XDocument ReadXaml(string relativePath)
        {
            return XDocument.Parse(RepositoryPath.ReadText(relativePath));
        }
    }
}
