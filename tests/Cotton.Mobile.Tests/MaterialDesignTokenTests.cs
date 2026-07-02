using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MaterialDesignTokenTests
    {
        private const string SpacingResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MSpacing.xaml";
        private const string InteractionResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MInteraction.xaml";
        private const string StylesResourcePath = "src/Cotton.Mobile/Resources/Styles/Styles.xaml";
        private const string ControlsDirectoryPath = "src/Cotton.Mobile/Controls";
        private const string MainPagePath = "src/Cotton.Mobile/MainPage.xaml";
        private const string TrashPagePath = "src/Cotton.Mobile/TrashPage.xaml";
        private const string MaterialDialogPagePath = "src/Cotton.Mobile/Controls/MaterialDialogPage.cs";
        private const string AppLockGatePagePath = "src/Cotton.Mobile/AppLockGatePage.xaml";
        private const string RecentFilesPagePath = "src/Cotton.Mobile/RecentFilesPage.xaml";
        private const string ActivityFeedPagePath = "src/Cotton.Mobile/ActivityFeedPage.xaml";
        private const string TransfersPagePath = "src/Cotton.Mobile/TransfersPage.xaml";
        private const string FileVersionHistoryPagePath = "src/Cotton.Mobile/FileVersionHistoryPage.xaml";
        private const string CaptureInboxPagePath = "src/Cotton.Mobile/CaptureInboxPage.xaml";
        private const string CaptureDestinationPickerPagePath = "src/Cotton.Mobile/CaptureDestinationPickerPage.xaml";
        private const string TextViewerPagePath = "src/Cotton.Mobile/TextViewerPage.xaml";
        private const string ImageViewerPagePath = "src/Cotton.Mobile/ImageViewerPage.xaml";
        private const string MediaViewerPagePath = "src/Cotton.Mobile/MediaViewerPage.xaml";
        private const string PdfViewerPagePath = "src/Cotton.Mobile/PdfViewerPage.xaml";
        private const string DiagnosticsPagePath = "src/Cotton.Mobile/DiagnosticsPage.xaml";
        private const string SyncSettingsPagePath = "src/Cotton.Mobile/SyncSettingsPage.xaml";
        private const string NotificationSettingsPagePath = "src/Cotton.Mobile/NotificationSettingsPage.xaml";
        private const string SecuritySettingsPagePath = "src/Cotton.Mobile/SecuritySettingsPage.xaml";
        private const string BackupSetupPagePath = "src/Cotton.Mobile/BackupSetupPage.xaml";
        private const string StoragePagePath = "src/Cotton.Mobile/StoragePage.xaml";
        private const string BrandHeaderViewPath = "src/Cotton.Mobile/Controls/BrandHeaderView.cs";
        private const string BrandMarkViewPath = "src/Cotton.Mobile/Controls/BrandMarkView.cs";
        private const string EmptyStateViewPath = "src/Cotton.Mobile/Controls/EmptyStateView.cs";
        private const string FileListMetadataViewPath = "src/Cotton.Mobile/Controls/FileListMetadataView.cs";
        private const string FileListEntryRowViewPath = "src/Cotton.Mobile/Controls/FileListEntryRowView.cs";
        private const string FileBrowserTopBarViewPath = "src/Cotton.Mobile/Controls/FileBrowserTopBarView.cs";
        private const string FloatingActionButtonViewPath = "src/Cotton.Mobile/Controls/FloatingActionButtonView.cs";
        private const string FileTileEntryCardViewPath = "src/Cotton.Mobile/Controls/FileTileEntryCardView.cs";
        private const string FileTileMetadataViewPath = "src/Cotton.Mobile/Controls/FileTileMetadataView.cs";
        private const string ContentCardViewPath = "src/Cotton.Mobile/Controls/ContentCardView.cs";
        private const string MetadataCardBodyViewPath = "src/Cotton.Mobile/Controls/MetadataCardBodyView.cs";
        private const string MetadataCardViewPath = "src/Cotton.Mobile/Controls/MetadataCardView.cs";
        private const string MetadataCardHeaderViewPath = "src/Cotton.Mobile/Controls/MetadataCardHeaderView.cs";
        private const string SettingsCardViewPath = "src/Cotton.Mobile/Controls/SettingsCardView.cs";
        private const string SettingsSummaryHeaderViewPath = "src/Cotton.Mobile/Controls/SettingsSummaryHeaderView.cs";
        private const string SettingsActionHeaderCardViewPath = "src/Cotton.Mobile/Controls/SettingsActionHeaderCardView.cs";
        private const string SettingsSectionHeaderViewPath = "src/Cotton.Mobile/Controls/SettingsSectionHeaderView.cs";
        private const string SettingsInfoItemViewPath = "src/Cotton.Mobile/Controls/SettingsInfoItemView.cs";
        private const string SettingsToggleItemViewPath = "src/Cotton.Mobile/Controls/SettingsToggleItemView.cs";
        private const string StorageBucketItemViewPath = "src/Cotton.Mobile/Controls/StorageBucketItemView.cs";
        private const string DiagnosticsItemViewPath = "src/Cotton.Mobile/Controls/DiagnosticsItemView.cs";
        private const string TextDocumentContentViewPath = "src/Cotton.Mobile/Controls/TextDocumentContentView.cs";
        private const string DocumentViewerBodyViewPath = "src/Cotton.Mobile/Controls/DocumentViewerBodyView.cs";
        private const string TrashEntryCardViewBasePath = "src/Cotton.Mobile/Controls/TrashEntryCardViewBase.cs";
        private const string TrashListEntryCardViewPath = "src/Cotton.Mobile/Controls/TrashListEntryCardView.cs";
        private const string TrashTileEntryCardViewPath = "src/Cotton.Mobile/Controls/TrashTileEntryCardView.cs";
        private const string LoadingStatusViewPath = "src/Cotton.Mobile/Controls/LoadingStatusView.cs";
        private const string ScreenContentGridViewPath = "src/Cotton.Mobile/Controls/ScreenContentGridView.cs";
        private const string ScreenScrollBodyViewPath = "src/Cotton.Mobile/Controls/ScreenScrollBodyView.cs";
        private const string ScreenStatusViewPath = "src/Cotton.Mobile/Controls/ScreenStatusView.cs";
        private const string NavigationBarViewPath = "src/Cotton.Mobile/Controls/NavigationBarView.cs";
        private const string NoticePanelViewPath = "src/Cotton.Mobile/Controls/NoticePanelView.cs";
        private const string LinearProgressViewPath = "src/Cotton.Mobile/Controls/LinearProgressView.cs";
        private const string SelectionOverlayViewPath = "src/Cotton.Mobile/Controls/SelectionOverlayView.cs";
        private const string TopAppBarPath = "src/Cotton.Mobile/Controls/TopAppBar.xaml";
        private const string ViewerInfoHeaderViewPath = "src/Cotton.Mobile/Controls/ViewerInfoHeaderView.cs";
        private const string DarkViewerSurfaceViewPath = "src/Cotton.Mobile/Controls/DarkViewerSurfaceView.cs";
        private const string ViewerStatusOverlayViewPath = "src/Cotton.Mobile/Controls/ViewerStatusOverlayView.cs";
        private const string ViewerPlayOverlayViewPath = "src/Cotton.Mobile/Controls/ViewerPlayOverlayView.cs";
        private const string ViewerOverlayActionButtonViewPath = "src/Cotton.Mobile/Controls/ViewerOverlayActionButtonView.cs";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";

        [Fact]
        public void Interactive_spacing_tokens_meet_android_touch_target()
        {
            XDocument spacing = LoadResourceDictionary(SpacingResourcePath);

            double touchTarget = GetDoubleResource(spacing, "TouchTarget");

            Assert.Equal(48, touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3ButtonMinSize") >= touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3ControlMinSize") >= touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3FooterLinkMinHeight") >= touchTarget);
            Assert.True(GetDoubleResource(spacing, "M3FilledButtonHeight") >= touchTarget);
        }

        [Fact]
        public void Material_switch_style_uses_shared_touch_target()
        {
            XDocument styles = LoadResourceDictionary(StylesResourcePath);

            IReadOnlyDictionary<string, string> switchSetters = GetStyleSetters(styles, "M3Switch");

            Assert.Equal("{StaticResource TouchTarget}", switchSetters["TouchTargetSize"]);
        }

        [Fact]
        public void Material_controls_with_dark_color_defaults_have_theme_aware_implicit_styles()
        {
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> darkDefaultProperties = GetControlDarkDefaultProperties();

            Assert.NotEmpty(darkDefaultProperties);

            foreach (KeyValuePair<string, IReadOnlyCollection<string>> control in darkDefaultProperties.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                string targetType = $"controls:{control.Key}";
                IReadOnlyDictionary<string, string> setters = GetImplicitStyleSetters(styles, targetType);

                foreach (string propertyName in control.Value.OrderBy(item => item, StringComparer.Ordinal))
                {
                    Assert.True(
                        setters.TryGetValue(propertyName, out string? setterValue),
                        $"{targetType}.{propertyName} must be set by the implicit Material style.");
                    Assert.StartsWith("{AppThemeBinding", setterValue, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void File_browser_loading_skeleton_uses_material_geometry_and_motion()
        {
            XDocument spacing = LoadResourceDictionary(SpacingResourcePath);
            XDocument interaction = LoadResourceDictionary(InteractionResourcePath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);

            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonLineHeight") > 0);
            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonSecondaryLineHeight") > 0);
            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonPrimaryLineWidth") > 0);
            Assert.True(GetDoubleResource(spacing, "M3FileSkeletonSecondaryLineWidth") > 0);
            Assert.True(GetDoubleResource(spacing, "M3MetadataSkeletonChipWidth") > 0);
            Assert.True(GetDoubleResource(spacing, "M3MetadataSkeletonChipHeight") > 0);
            Assert.True(GetDoubleResource(spacing, "M3MetadataSkeletonBodyLineWidth") > 0);
            Assert.True(GetDoubleResource(interaction, "M3SkeletonIdleOpacity") < 1);
            Assert.True(GetDoubleResource(interaction, "M3SkeletonPulseOpacity") > GetDoubleResource(interaction, "M3SkeletonIdleOpacity"));
            Assert.InRange(GetIntResource(interaction, "M3MotionSkeletonEnterDuration"), 120, 220);
            Assert.True(GetIntResource(interaction, "M3MotionSkeletonPulseDuration") >= 1000);

            IReadOnlyDictionary<string, string> skeletonSetters = GetStyleSetters(styles, "M3SkeletonBlock");
            IReadOnlyDictionary<string, string> listSetters = GetStyleSetters(styles, "M3FileListSkeletonView");
            IReadOnlyDictionary<string, string> metadataListSetters = GetStyleSetters(styles, "M3MetadataListSkeletonView");
            IReadOnlyDictionary<string, string> metadataGridSetters = GetStyleSetters(styles, "M3MetadataSkeletonGrid");
            IReadOnlyDictionary<string, string> metadataChipSetters = GetStyleSetters(styles, "M3MetadataSkeletonChipBlock");
            IReadOnlyDictionary<string, string> rowSetters = GetStyleSetters(styles, "M3FileSkeletonRowGrid");

            Assert.Equal("{AppThemeBinding Light={StaticResource M3LightSurfaceContainerHigh}, Dark={StaticResource M3DarkSurfaceContainerHigh}}", skeletonSetters["BackgroundColor"]);
            Assert.Equal("{StaticResource M3SkeletonIdleOpacity}", skeletonSetters["IdleOpacity"]);
            Assert.Equal("{StaticResource M3SkeletonPulseOpacity}", skeletonSetters["PulseOpacity"]);
            Assert.Equal("{StaticResource M3MotionSkeletonPulseDuration}", skeletonSetters["PulseDuration"]);
            Assert.Equal("{StaticResource SpaceNone}", listSetters["Spacing"]);
            Assert.Equal("True", listSetters["InputTransparent"]);
            Assert.Equal("{StaticResource M3MotionSkeletonEnterDuration}", listSetters["AppearanceDuration"]);
            Assert.Equal("{StaticResource M3CardListItemSpacing}", metadataListSetters["Spacing"]);
            Assert.Equal("True", metadataListSetters["InputTransparent"]);
            Assert.Equal("{StaticResource M3MotionSkeletonEnterDuration}", metadataListSetters["AppearanceDuration"]);
            Assert.Equal("{StaticResource Space8}", metadataGridSetters["RowSpacing"]);
            Assert.Equal("{StaticResource Space12}", metadataGridSetters["ColumnSpacing"]);
            Assert.Equal("{StaticResource M3MetadataSkeletonChipWidth}", metadataChipSetters["WidthRequest"]);
            Assert.Equal("{StaticResource M3MetadataSkeletonChipHeight}", metadataChipSetters["HeightRequest"]);
            Assert.Equal("{StaticResource M3FileRowPadding}", rowSetters["Padding"]);
            Assert.Equal("{StaticResource M3FileRowHeight}", rowSetters["HeightRequest"]);
            Assert.Equal("{StaticResource Space12}", rowSetters["ColumnSpacing"]);
        }

        [Fact]
        public void Main_file_browser_uses_reusable_loading_skeleton_view()
        {
            string mainPage = LoadText(MainPagePath);

            Assert.Contains("<controls:FileListSkeletonView", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonRowGrid", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonPrimaryLineBlock", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonSecondaryLineBlock", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_list_screens_use_initial_loading_skeletons()
        {
            string[] screenPaths =
            [
                RecentFilesPagePath,
                ActivityFeedPagePath,
                TransfersPagePath,
                FileVersionHistoryPagePath,
                CaptureInboxPagePath,
            ];

            foreach (string screenPath in screenPaths)
            {
                string page = LoadText(screenPath);

                Assert.Contains("<controls:MetadataListSkeletonView", page, StringComparison.Ordinal);
                Assert.Contains("IsLoadingPlaceholderVisible", page, StringComparison.Ordinal);
                Assert.Contains("M3MetadataListSkeletonView", page, StringComparison.Ordinal);
            }

            string recentFilesPage = LoadText(RecentFilesPagePath);

            Assert.Contains("IsBodyLineVisible=\"False\"", recentFilesPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Folder_picker_screens_use_file_loading_skeletons()
        {
            string destinationPickerPage = LoadText(CaptureDestinationPickerPagePath);

            Assert.Contains("<controls:FileListSkeletonView", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("IsLoadingPlaceholderVisible", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("M3FileListSkeletonView", destinationPickerPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Outlined_inputs_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string materialDialogPage = LoadText(MaterialDialogPagePath);

            Assert.Contains("<controls:OutlinedInputField", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:OutlinedInputField", trashPage, StringComparison.Ordinal);
            Assert.Contains("OutlinedInputField", materialDialogPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Entry", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Entry", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("FocusedInputChromeBehavior", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("FocusedInputChromeBehavior", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_empty_states_use_reusable_material_control()
        {
            string[] screenPaths =
            [
                MainPagePath,
                RecentFilesPagePath,
                ActivityFeedPagePath,
                TransfersPagePath,
                FileVersionHistoryPagePath,
                CaptureInboxPagePath,
                CaptureDestinationPickerPagePath,
                TrashPagePath,
                SyncSettingsPagePath,
                PdfViewerPagePath,
                AppLockGatePagePath,
            ];

            foreach (string screenPath in screenPaths)
            {
                string page = LoadText(screenPath);

                Assert.Contains("<controls:EmptyStateView", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3EmptyStateStack", page, StringComparison.Ordinal);
            }

            string mainPage = LoadText(MainPagePath);
            string syncSettingsPage = LoadText(SyncSettingsPagePath);
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string appLockGatePage = LoadText(AppLockGatePagePath);
            string emptyStateView = LoadText(EmptyStateViewPath);
            string styles = LoadText(StylesResourcePath);

            Assert.Contains("ActionCommand=\"{Binding ShowFileAddActionsCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsBodyVisible=\"{Binding Display.IsFilesEmptyDetailsVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ActionText=\"Choose folder\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("ActionIconButtonStyleResourceKey=\"M3PrimaryFileChromeIconButton\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey=\"M3CenteredPdfEmptyStateCard\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("IconFrameStyleResourceKey=\"M3PdfEmptyStateIconFrame\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:EmptyStateView Grid.Row=\"1\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("IsBusy=\"{Binding IsBusy}\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("IsActionEnabled=\"{Binding CanUnlock}\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("IsFilledAction=\"True\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey=\"M3AppLockCard\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("IsBusyProperty", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("IsFilledActionProperty", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("FilledActionButtonStyleResourceKeyProperty", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("new LoadingIndicatorView", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("new FilledButton", emptyStateView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.Row=\"1\"", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3AppLockContentStack", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3AppLockContentStack", styles, StringComparison.Ordinal);
        }

        [Fact]
        public void Auth_brand_header_uses_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string brandHeaderView = LoadText(BrandHeaderViewPath);
            string brandMarkView = LoadText(BrandMarkViewPath);

            Assert.Contains("<controls:BrandHeaderView Source=\"cotton_brand_mark.svg\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"Cotton Cloud\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"Cotton Cloud\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class BrandHeaderView", brandHeaderView, StringComparison.Ordinal);
            Assert.Contains("new BrandMarkView", brandHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3AuthBrandGrid\"", brandHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultTitleStyleResourceKey = \"M3AuthTitle\"", brandHeaderView, StringComparison.Ordinal);
            Assert.Contains("SemanticProperties.SetHeadingLevel(_titleLabel, SemanticHeadingLevel.Level1)", brandHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultFrameStyleResourceKey = \"M3AuthBrandMarkFrame\"", brandMarkView, StringComparison.Ordinal);
            Assert.Contains("DefaultImageStyleResourceKey = \"M3AuthBrandMarkImage\"", brandMarkView, StringComparison.Ordinal);
            Assert.Contains("_frame.SetDynamicResource(StyleProperty, frameStyleResourceKey)", brandMarkView, StringComparison.Ordinal);
            Assert.Contains("_image.SetDynamicResource(StyleProperty, imageStyleResourceKey)", brandMarkView, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:BrandMarkView", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthBrandGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthTitle}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.HeadingLevel=\"Level1\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Style=\"{StaticResource M3AuthBrandMarkFrame}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthBrandMarkImage}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Loading_indicator_frames_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string appLockGatePage = LoadText(AppLockGatePagePath);
            string loadingStatusView = LoadText(LoadingStatusViewPath);

            Assert.Equal(3, CountOccurrences(mainPage, "<controls:LoadingStatusView"));
            Assert.Contains("<controls:LoadingStatusView", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding Display.IsLoadingVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ContainerStyleResourceKey=\"M3AuthLoadingStatusPanel\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"Waiting for browser approval\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("DetailText=\"{Binding Display.AuthorizationProgressMessage}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ActionCommand=\"{Binding CancelAuthorizationCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ActionCommand=\"{Binding CancelFileActionCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ActionSemanticDescription=\"Cancel file operation\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsBusy=\"{Binding IsBusy}\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("DetailTextProperty", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains("ContainerStyleResourceKeyProperty", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains("TextStyleResourceKeyProperty", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains("new LoadingIndicatorView", loadingStatusView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border IsVisible=\"{Binding Display.IsLoadingVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border IsVisible=\"{Binding Display.IsAuthorizationProgressVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingStatusPanel", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingIndicatorFrame", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingActivityIndicator", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingIndicatorFrame", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingActivityIndicator", appLockGatePage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_screen_status_text_uses_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string screenStatusView = LoadText(ScreenStatusViewPath);

            Assert.Contains("<controls:ScreenStatusView Text=\"{Binding Display.Status}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("TextStyleResourceKey=\"M3AuthStatus\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenStatusView Text=\"{Binding Display.ProfileStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("TextStyleResourceKey=\"M3BodyMedium\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("TextStyleResourceKeyProperty", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("DefaultTextStyleResourceKey = \"M3ScreenStatus\"", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("_label.SetDynamicResource(StyleProperty, textStyleResourceKey)", screenStatusView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Display.Status}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Display.ProfileStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3BodyMedium}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Retry_attention_panels_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);

            Assert.Contains("<controls:AttentionStatusView", mainPage, StringComparison.Ordinal);
            Assert.Contains("ActionIconButtonStyleResourceKey=\"M3DestructiveFileChromeIconButton\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3AttentionStatusPanel", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3AttentionStatusMessage", mainPage, StringComparison.Ordinal);

            Assert.Contains("<controls:AttentionStatusView", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsRowTapEnabled=\"True\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("GridStyleResourceKey=\"M3ActionListItemGrid\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Retry notifications\"", notificationSettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void File_notice_panels_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string noticePanelView = LoadText(NoticePanelViewPath);

            Assert.Contains("<controls:NoticePanelView IsVisible=\"{Binding Display.IsFilesNoticeVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IconData=\"{x:Static controls:IconPathData.Error}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding Display.FilesNoticeTitle}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Message=\"{Binding Display.FilesNoticeMessage}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("DefaultPanelStyleResourceKey = \"M3FileNoticePanel\"", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3FileNoticeGrid\"", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("DefaultIconFrameStyleResourceKey = \"M3FileNoticeIconFrame\"", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("DefaultTextStackStyleResourceKey = \"M3FileNoticeTextStack\"", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("<controls:NoticePanelView IsVisible=\"{Binding IsRemotePushUnavailable}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding RemotePushUnavailableTitle}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("Message=\"{Binding RemotePushUnavailableDetail}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("PanelStyleResourceKey=\"M3AttentionStatusPanel\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IconFrameStyleResourceKey=\"M3AttentionNoticeIconFrame\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("ActionText=\"Retry server push\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("ActionCommand=\"{Binding LoadCommand}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("ActionTextProperty", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("new ActionListItemView", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("_actionItem.Command = actionCommand", noticePanelView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border IsVisible=\"{Binding Display.IsFilesNoticeVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticePanel", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticeGrid", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticeIconFrame", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticeTextStack", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid IsVisible=\"{Binding IsRemotePushUnavailable}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionListItemView Grid.Row=\"2\"", notificationSettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Action_rows_use_reusable_material_control()
        {
            string activityFeedPage = LoadText(ActivityFeedPagePath);
            string backupSetupPage = LoadText(BackupSetupPagePath);
            string captureDestinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string recentFilesPage = LoadText(RecentFilesPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string storagePage = LoadText(StoragePagePath);

            Assert.Contains("<controls:ActionListItemView Text=\"Load more\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"Load more activity\"", activityFeedPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<behaviors:LongPressBehavior", activityFeedPage, StringComparison.Ordinal);

            Assert.Contains("<controls:ActionListItemView Text=\"{Binding MediaAccessActionText}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding MediaAccessActionText}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding MediaAccessActionCommand}\"", backupSetupPage, StringComparison.Ordinal);

            Assert.Contains("<controls:ActionListItemView Text=\"{Binding DisplayName}\"", captureDestinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("ActionIconData=\"{x:Static controls:IconPathData.ChevronRight}\"", captureDestinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"{Binding DisplayName, StringFormat='Open {0}'}\"", captureDestinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<behaviors:LongPressBehavior", captureDestinationPickerPage, StringComparison.Ordinal);

            Assert.Contains("<controls:ActionListItemView Text=\"{Binding FileName}\"", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding BadgeText}\"", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("RowTapCommand=\"{Binding BindingContext.OpenRecentFileCommand, Source={x:Reference RecentFilesRoot}}\"", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("CommandParameter=\"{Binding .}\"", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("ActionSemanticDescription=\"Remove recent file\"", recentFilesPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<behaviors:LongPressBehavior", recentFilesPage, StringComparison.Ordinal);

            Assert.Equal(1, CountOccurrences(notificationSettingsPage, "<controls:ActionListItemView"));
            Assert.Contains("LeadingIconFrameStyleResourceKey=\"M3CardActivityThumbnailFrame\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding PermissionActionText}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Retry server push\"", notificationSettingsPage, StringComparison.Ordinal);

            Assert.Equal(2, CountOccurrences(securitySettingsPage, "<controls:ActionListItemView"));
            Assert.Contains("Text=\"{Binding DeviceUnlockActionText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsActionEnabled=\"{Binding CanVerifyDeviceUnlock}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding RevokeCurrentSessionActionText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("ActionIconButtonStyleResourceKey=\"M3DestructiveFileChromeIconButton\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<behaviors:LongPressBehavior", securitySettingsPage, StringComparison.Ordinal);

            Assert.Equal(2, CountOccurrences(storagePage, "<controls:ActionListItemView"));
            Assert.Contains("SupportingText=\"Remove evictable local copies while keeping offline files.\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("ActionIconButtonStyleResourceKey=\"M3DestructiveFileChromeIconButton\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<behaviors:LongPressBehavior", storagePage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_browser_header_actions_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileBrowserTopBarView = LoadText(FileBrowserTopBarViewPath);

            Assert.Equal(1, CountOccurrences(mainPage, "<controls:FileBrowserTopBarView"));
            Assert.Contains("Title=\"{Binding Display.FilesTitle}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("PathText=\"{Binding Display.FilesPath}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("SearchCommand=\"{Binding ToggleFileSearchCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("SearchSemanticDescription=\"{Binding Display.FileSearchButtonDescription}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsSearchActive=\"{Binding Display.IsFileSearchVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("SortCommand=\"{Binding ShowFileSortActionsCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsSortVisible=\"{Binding Display.IsFileSortButtonVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ViewCommand=\"{Binding ShowFileViewActionsCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsViewVisible=\"{Binding Display.IsFileViewButtonVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ProfileInitials=\"{Binding Display.ProfileInitials}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class FileBrowserTopBarView", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3FileBrowserTopBar\"", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultActionClusterStyleResourceKey = \"M3FileBrowserActionCluster\"", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("new ActionClusterView", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("new InitialsButton", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("IsSearchActive ? IconPathData.Close : IconPathData.Search", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid ColumnDefinitions=\"Auto,*,Auto\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileBrowserTopBar}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionClusterView ClusterStyleResourceKey=\"M3FileBrowserActionCluster\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:InitialsButton Text=\"{Binding Display.ProfileInitials}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Search}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Sort}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.ViewTiles}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"controls:ActionClusterView\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Property=\"PrimaryActionIconData\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Capture_inbox_action_bar_uses_reusable_material_control()
        {
            string captureInboxPage = LoadText(CaptureInboxPagePath);
            string styles = LoadText(StylesResourcePath);

            Assert.Equal(1, CountOccurrences(captureInboxPage, "<controls:ActionClusterView ClusterStyleResourceKey=\"M3InlineActionCluster\""));
            Assert.Contains("x:Key=\"M3InlineActionCluster\"", styles, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding DestinationCommand}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionSemanticDescription=\"Choose destination\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding RenameCommand}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionSemanticDescription=\"Rename capture item\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionCommand=\"{Binding EnqueueCommand}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionIconButtonStyleResourceKey=\"M3PrimaryFileChromeIconButton\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionSemanticDescription=\"Queue captured items\"", captureInboxPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3InlineActionBarGrid", captureInboxPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3InlineActionBarGrid", styles, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid ColumnDefinitions=\"Auto,Auto,Auto,*\"", captureInboxPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Folder}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton Grid.Column=\"1\"", captureInboxPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton Grid.Column=\"2\"", captureInboxPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Capture_destination_current_folder_actions_use_reusable_material_control()
        {
            string destinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string settingsActionHeaderCardView = LoadText(SettingsActionHeaderCardViewPath);

            Assert.Contains("<controls:SettingsActionHeaderCardView LeadingIconData=\"{x:Static controls:IconPathData.Folder}\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("public class SettingsActionHeaderCardView", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("new SettingsSectionHeaderView", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("new ActionClusterView", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultActionClusterStyleResourceKey = \"M3InlineActionCluster\"", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding UpCommand}\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionSemanticDescription=\"Go to parent folder\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding ChooseCommand}\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionIconButtonStyleResourceKey=\"M3PrimaryFileChromeIconButton\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionSemanticDescription=\"Choose current folder\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:SettingsSectionHeaderView LeadingIconData", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:SettingsSectionHeaderView.TrailingContent>", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid ColumnDefinitions=\"Auto,*,Auto\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ColumnDefinitions=\"Auto,*,Auto,Auto\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton Grid.Column=\"2\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton Grid.Column=\"3\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Go to parent folder\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Choose current folder\"", destinationPickerPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Backup_setup_card_actions_use_reusable_material_control()
        {
            string backupSetupPage = LoadText(BackupSetupPagePath);
            string settingsActionHeaderCardView = LoadText(SettingsActionHeaderCardViewPath);
            string settingsSectionHeaderView = LoadText(SettingsSectionHeaderViewPath);

            Assert.Equal(2, CountOccurrences(backupSetupPage, "<controls:SettingsActionHeaderCardView LeadingIconData"));
            Assert.Equal(1, CountOccurrences(backupSetupPage, "<controls:SettingsSectionHeaderView LeadingIconData"));
            Assert.Contains("DefaultActionClusterStyleResourceKey = \"M3InlineActionCluster\"", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("SecondaryDetailTextProperty", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("TertiaryDetailTextProperty", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("QuaternaryDetailTextProperty", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("TapCommandProperty", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("_header.TapCommand = TapCommand", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("_header.IsTapEnabled = IsTapEnabled", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("TrailingContentProperty", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("TrailingTextProperty", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("new ChipView", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("trailingContent = TrailingContent ?? (isTrailingTextVisible ? _trailingChip : null)", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("QuaternaryDetailTextProperty", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("_trailingContentHost.Content = trailingContent", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding MediaAccessStatusText}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsTrailingTextVisible=\"True\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailTextStyleResourceKey=\"M3CardSupportingBlock\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryDetailTextStyleResourceKey=\"M3CardSupportingLine\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryDetailTextStyleResourceKey=\"M3CardSupportingStrongLine\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailTextStyleResourceKey=\"M3CardSupportingStrongBlock\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(backupSetupPage, "IsTapEnabled=\"True\""));
            Assert.Contains("PrimaryActionCommand=\"{Binding ChooseDestinationCommand}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionSemanticDescription=\"Choose backup destination\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding QueueNowCommand}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionSemanticDescription=\"Queue camera backup now\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("TapCommand=\"{Binding ChooseDestinationCommand}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("TapCommand=\"{Binding QueueNowCommand}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ClusterStyleResourceKey=\"M3InlineActionCluster\"", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:SettingsSectionHeaderView.TrailingContent>", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:TouchSurfaceView", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,Auto\"", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,Auto,Auto\"", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton Grid.Column=\"2\"", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Choose backup destination\"", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Queue camera backup now\"", backupSetupPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Top_app_bar_actions_use_reusable_material_control()
        {
            string topAppBar = LoadText(TopAppBarPath);

            Assert.Equal(1, CountOccurrences(topAppBar, "<controls:IconButton IconData="));
            Assert.Contains("<controls:ActionClusterView Grid.Column=\"2\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("ClusterStyleResourceKey=\"M3TopAppBarActionCluster\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionIconData=\"{Binding Source={x:Reference Root}, Path=PrimaryIconData}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding Source={x:Reference Root}, Path=PrimaryCommand}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("IsPrimaryActionVisible=\"{Binding Source={x:Reference Root}, Path=IsPrimaryActionVisible}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding Source={x:Reference Root}, Path=SecondaryCommand}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("IsSecondaryActionVisible=\"{Binding Source={x:Reference Root}, Path=IsSecondaryActionVisible}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionCommand=\"{Binding Source={x:Reference Root}, Path=TertiaryCommand}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("IsTertiaryActionVisible=\"{Binding Source={x:Reference Root}, Path=IsTertiaryActionVisible}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("TargetType=\"controls:ActionClusterView\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("Property=\"PrimaryActionIconButtonStyleResourceKey\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("Property=\"SecondaryActionIconButtonStyleResourceKey\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("Property=\"TertiaryActionIconButtonStyleResourceKey\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<HorizontalStackLayout Grid.Column=\"2\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Source={x:Reference Root}, Path=PrimaryDescription}\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Source={x:Reference Root}, Path=SecondaryDescription}\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Source={x:Reference Root}, Path=TertiaryDescription}\"", topAppBar, StringComparison.Ordinal);
        }

        [Fact]
        public void File_status_action_rows_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);

            Assert.Equal(3, CountOccurrences(mainPage, "<controls:FileStatusActionView"));
            Assert.Contains("Command=\"{Binding OpenTransfersCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsError=\"{Binding Display.TransferActivityIndicator.HasFailures}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding OpenBackupSetupCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding Display.OfflinePackProgress.Text}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IconData=\"{x:Static controls:IconPathData.Download}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TapCommand=\"{Binding OpenTransfersCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TapCommand=\"{Binding OpenBackupSetupCommand}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_list_rows_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);

            Assert.Contains("<controls:FileListEntryRowView Title=\"{Binding Name}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Detail=\"{Binding DisplayDetails}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class FileListEntryRowView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("_grid.SetDynamicResource(StyleProperty, \"M3FileListRowGrid\")", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<double>(\"M3FileListThumbnailColumnWidth\")", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<double>(\"M3FileActionSize\")", fileListEntryRowView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Style=\"{StaticResource M3FileListRowGrid}\">", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ColumnDefinition Width=\"{StaticResource M3FileListThumbnailColumnWidth}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ColumnDefinition Width=\"{StaticResource M3FileActionSize}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Selection_bars_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);

            Assert.Contains("<controls:SelectionBarView IsVisible=\"{Binding Display.IsFileSelectionBarVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding ShowFileSelectionActionsCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding ClearFileSelectionCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3SelectionBar", mainPage, StringComparison.Ordinal);

            Assert.Contains("<controls:SelectionBarView Grid.Row=\"3\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding RestoreSelectionCommand}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding DeleteForeverSelectionCommand}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionIconButtonStyleResourceKey=\"M3DestructiveFileChromeIconButton\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionCommand=\"{Binding CancelSelectionCommand}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3SelectionBar", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Selection_overlays_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string selectionOverlayView = LoadText(SelectionOverlayViewPath);

            Assert.Equal(0, CountOccurrences(mainPage, "<controls:SelectionOverlayView"));
            Assert.Contains("new SelectionOverlayView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("new SelectionOverlayView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumnSpan(_selectionOverlay, 3)", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("Grid.SetRowSpan(_selectionOverlay, 2)", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("OverlayStyleResourceKey = \"M3FileSelectionRowOverlay\"", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("DefaultOverlayStyleResourceKey = \"M3FileSelectionOverlay\"", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("InputTransparent = true", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("_overlay.IsVisible = IsSelected", selectionOverlayView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.ColumnSpan=\"3\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.RowSpan=\"2\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileSelectionRowOverlay}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileSelectionOverlay}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Bottom_navigation_uses_reusable_material_shell()
        {
            string mainPage = LoadText(MainPagePath);
            string navigationBarView = LoadText(NavigationBarViewPath);

            Assert.Contains("<controls:NavigationBarView Grid.Row=\"2\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding Display.IsFileBrowserQuickNavigationVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Equal(4, CountOccurrences(mainPage, "<controls:NavigationBarItem"));
            Assert.Contains("DefaultColumnCount = 4", navigationBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3NavigationBarGrid\"", navigationBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultSurfaceStyleResourceKey = \"M3NavigationBarSurface\"", navigationBarView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _grid.Children", navigationBarView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.Row=\"2\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NavigationBarSurface}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NavigationBarGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ColumnDefinitions=\"*,*,*,*\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void File_entry_thumbnails_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string trashListEntryCardView = LoadText(TrashListEntryCardViewPath);
            string trashTileEntryCardView = LoadText(TrashTileEntryCardViewPath);

            Assert.Equal(0, CountOccurrences(mainPage, "<controls:FileThumbnailView"));
            Assert.Contains("SurfaceStyleResourceKey = \"M3FilePreviewSurface\"", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("SelectionMarkStyleResourceKey = \"M3FileTileSelectionMark\"", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("FolderIconSize=\"{Binding Source={x:Reference RootPage}, Path=FileTileFolderIconSize}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("new FileThumbnailView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("new FileThumbnailView", fileTileEntryCardView, StringComparison.Ordinal);

            Assert.DoesNotContain("<controls:FileThumbnailView", trashPage, StringComparison.Ordinal);
            Assert.Contains("new FileThumbnailView", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new FileThumbnailView", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_thumbnail.SurfaceStyleResourceKey = \"M3MetadataFileThumbnailSurface\"", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("SurfaceStyleResourceKey = \"M3TrashTilePreviewSurface\"", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("SelectionMarkStyleResourceKey = \"M3FileTileSelectionMark\"", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("IsBadgeVisible = true", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_thumbnail.BadgeText = BadgeText", trashTileEntryCardView, StringComparison.Ordinal);

            string[] pages =
            [
                mainPage,
                trashPage,
            ];

            foreach (string page in pages)
            {
                Assert.DoesNotContain("<Image Source=\"{Binding Thumbnail.Source}\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3ThumbnailActivityIndicator", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3DynamicThumbnailPlaceholder", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3FileSelectionCheckIcon", page, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Touch_surfaces_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string backupSetupPage = LoadText(BackupSetupPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string settingsSectionHeaderView = LoadText(SettingsSectionHeaderViewPath);
            string trashEntryCardViewBase = LoadText(TrashEntryCardViewBasePath);
            string trashListEntryCardView = LoadText(TrashListEntryCardViewPath);
            string trashTileEntryCardView = LoadText(TrashTileEntryCardViewPath);

            Assert.Equal(0, CountOccurrences(mainPage, "<controls:TouchSurfaceView"));
            Assert.Equal(2, CountOccurrences(mainPage, "BeginSelectionCommand=\"{Binding BindingContext.BeginFileSelectionCommand, Source={x:Reference RootPage}}\""));
            Assert.Equal(2, CountOccurrences(mainPage, "ActivateCommand=\"{Binding BindingContext.ActivateFileBrowserEntryCommand, Source={x:Reference RootPage}}\""));
            Assert.Contains("CommandParameter=\"{Binding .}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("new TouchSurfaceView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("new TouchSurfaceView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.Command = BeginSelectionCommand", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.Command = BeginSelectionCommand", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.TapCommand = ActivateCommand", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.TapCommand = ActivateCommand", fileTileEntryCardView, StringComparison.Ordinal);

            Assert.DoesNotContain("<controls:TouchSurfaceView", trashPage, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(trashPage, "ToggleSelectionCommand=\"{Binding BindingContext.ToggleSelectionCommand, Source={x:Reference TrashRoot}}\""));
            Assert.Contains("new TouchSurfaceView", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new TouchSurfaceView", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("touchSurface.TapCommand = ToggleSelectionCommand", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("touchSurface.TapCommandParameter = CommandParameter", trashEntryCardViewBase, StringComparison.Ordinal);

            Assert.DoesNotContain("<controls:TouchSurfaceView", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("new TouchSurfaceView", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("TapCommandProperty", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("TapCommand=\"{Binding ChooseDestinationCommand}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("TapCommand=\"{Binding QueueNowCommand}\"", backupSetupPage, StringComparison.Ordinal);

            string[] pages =
            [
                mainPage,
                trashPage,
            ];

            foreach (string page in pages)
            {
                Assert.DoesNotContain("xmlns:behaviors", page, StringComparison.Ordinal);
                Assert.DoesNotContain("<behaviors:LongPressBehavior", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3ListItemTouchSurface", page, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void File_status_chips_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string backupSetupPage = LoadText(BackupSetupPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string fileListMetadataView = LoadText(FileListMetadataViewPath);
            string fileTileMetadataView = LoadText(FileTileMetadataViewPath);
            string settingsSectionHeaderView = LoadText(SettingsSectionHeaderViewPath);
            string settingsInfoItemView = LoadText(SettingsInfoItemViewPath);
            string trashListEntryCardView = LoadText(TrashListEntryCardViewPath);
            string trashTileEntryCardView = LoadText(TrashTileEntryCardViewPath);

            Assert.DoesNotContain("<controls:ChipView", mainPage, StringComparison.Ordinal);
            Assert.Contains("new ChipView", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultTrailingChipStyleResourceKey = \"M3NeutralChip\"", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("new ChipView", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultLocalChipStyleResourceKey = \"M3AccentOutlineChip\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultOfflineChipStyleResourceKey = \"M3FileAttentionChip\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AccentOutlineChip}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileAttentionChip}\"", mainPage, StringComparison.Ordinal);

            Assert.DoesNotContain("<controls:ChipView", trashPage, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(trashPage, "BadgeText=\"{Binding BadgeText}\""));
            Assert.Contains("_metadata.TrailingText = BadgeText", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_thumbnail.BadgeText = BadgeText", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NeutralChip}\"", trashPage, StringComparison.Ordinal);

            Assert.DoesNotContain("<controls:ChipView", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding MediaAccessStatusText}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("new ChipView", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultTrailingChipStyleResourceKey = \"M3NeutralChip\"", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NeutralChip}\"", backupSetupPage, StringComparison.Ordinal);

            Assert.DoesNotContain("<controls:ChipView", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding StatusText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding BadgeText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("new ChipView", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultTrailingChipStyleResourceKey = \"M3TrailingChip\"", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultAttentionTrailingTextStyleResourceKey = \"M3ErrorChipLabel\"", settingsInfoItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3TrailingChip}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding StatusText}\"", securitySettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_tile_metadata_uses_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string fileTileMetadataView = LoadText(FileTileMetadataViewPath);

            Assert.Contains("<controls:FileTileEntryCardView Title=\"{Binding Name}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding Name}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Detail=\"{Binding Details}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("LocalCopyStatus=\"{Binding LocalCopyStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsLocalCopyVisible=\"{Binding HasLocalCopy}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("OfflineAttentionStatus=\"{Binding OfflineAttentionStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsOfflineAttentionVisible=\"{Binding IsOfflineAttentionVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("new FileTileMetadataView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_slotGrid.SetDynamicResource(StyleProperty, \"M3FileTileSlotGrid\")", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_contentGrid.SetDynamicResource(StyleProperty, \"M3FileTileContentGrid\")", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_previewRow.Height = new GridLength(PreviewHeight)", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_thumbnail.HeightRequest = PreviewHeight", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_metadata.LocalCopyStatus = LocalCopyStatus ?? string.Empty", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3FileTileTextStack\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultMetadataGridStyleResourceKey = \"M3FileTileMetadataGrid\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultTitleStyleResourceKey = \"M3CardSupportingStrongLine\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultDetailStyleResourceKey = \"M3CardMetaLine\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileSlotGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileContentGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<RowDefinition Height=\"{Binding Source={x:Reference RootPage}, Path=FileTilePreviewHeight}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileTextStack}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileMetadataGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ChipStyleResourceKey=\"M3AccentOutlineChip\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ChipStyleResourceKey=\"M3FileAttentionChip\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Metadata_cards_use_reusable_material_control()
        {
            string activityFeedPage = LoadText(ActivityFeedPagePath);
            string fileVersionHistoryPage = LoadText(FileVersionHistoryPagePath);
            string transfersPage = LoadText(TransfersPagePath);
            string captureInboxPage = LoadText(CaptureInboxPagePath);
            string metadataCardBodyView = LoadText(MetadataCardBodyViewPath);
            string metadataCardView = LoadText(MetadataCardViewPath);
            string metadataCardHeaderView = LoadText(MetadataCardHeaderViewPath);
            string styles = LoadText(StylesResourcePath);

            Assert.Contains("<controls:MetadataCardView LeadingIconData=\"{x:Static controls:IconPathData.Activity}\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("LeadingIconFrameStyleResourceKey=\"M3CardActivityThumbnailFrame\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding Title}\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("SupportingText=\"{Binding DetailText}\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding BadgeText}\"", activityFeedPage, StringComparison.Ordinal);

            Assert.Contains("<controls:MetadataCardView LeadingIconData=\"{x:Static controls:IconPathData.File}\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("LeadingIconFrameStyleResourceKey=\"M3CardFileThumbnailFrame\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding VersionText}\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("SupportingText=\"{Binding Name}\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding KindText}\"", fileVersionHistoryPage, StringComparison.Ordinal);

            Assert.Contains("<controls:MetadataCardView LeadingIconData=\"{x:Static controls:IconPathData.Transfer}\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("LeadingIconFrameStyleResourceKey=\"M3CardTransferThumbnailFrame\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding DisplayName}\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("SupportingText=\"{Binding DetailText}\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding StatusText}\"", transfersPage, StringComparison.Ordinal);

            Assert.Contains("<controls:MetadataCardView LeadingIconData=\"{x:Static controls:IconPathData.Transfer}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("LeadingIconFrameStyleResourceKey=\"M3CardCaptureThumbnailFrame\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding DisplayName}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("SupportingText=\"{Binding DetailText}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding StatusText}\"", captureInboxPage, StringComparison.Ordinal);

            Assert.Contains("new MetadataCardHeaderView", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultCardStyleResourceKey = \"M3ContentCard\"", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3MetadataCardGrid\"", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<double>(\"M3FileThumbnailSize\")", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumnSpan(_header, 3)", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("new FileEntryTextView", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("new ChipView", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3MetadataCardGrid\"", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultTrailingChipStyleResourceKey = \"M3NeutralChip\"", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("x:Key=\"M3MetadataCardBodyStack\"", styles, StringComparison.Ordinal);
            Assert.Contains("public class MetadataCardBodyView", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3MetadataCardBodyStack\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultInlineGridStyleResourceKey = \"M3InlineMetadataGrid\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("new LinearProgressView", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Equal(4, CountOccurrences(activityFeedPage + fileVersionHistoryPage + transfersPage + captureInboxPage, "<controls:MetadataCardView"));
            Assert.Equal(4, CountOccurrences(activityFeedPage + fileVersionHistoryPage + transfersPage + captureInboxPage, "<controls:MetadataCardBodyView"));
            Assert.Contains("PrimaryText=\"{Binding ContentText}\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryText=\"{Binding ContentTypeText}\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("Progress=\"{Binding ProgressFraction}\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("LeadingInlineText=\"{Binding KindText}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("TrailingInlineText=\"{Binding MetadataText}\"", captureInboxPage, StringComparison.Ordinal);

            string[] metadataCardPages =
            [
                activityFeedPage,
                fileVersionHistoryPage,
                transfersPage,
                captureInboxPage,
            ];

            foreach (string page in metadataCardPages)
            {
                Assert.DoesNotContain("<controls:MetadataCardHeaderView", page, StringComparison.Ordinal);
                Assert.DoesNotContain("Style=\"{StaticResource M3MetadataCardGrid}\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("Width=\"{StaticResource M3FileThumbnailSize}\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("Grid.RowSpan=\"3\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("Grid.RowSpan=\"4\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("ColumnDefinitions=\"Auto,*,Auto\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("Style=\"{StaticResource M3MetadataCardBodyStack}\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("Style=\"{StaticResource M3InlineMetadataGrid}\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("Style=\"{StaticResource M3NeutralChip}\"", page, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Trash_header_actions_use_reusable_material_control()
        {
            string trashPage = LoadText(TrashPagePath);

            Assert.Equal(1, CountOccurrences(trashPage, "<controls:ActionClusterView ClusterStyleResourceKey=\"M3ScreenHeaderActionCluster\""));
            Assert.Contains("<controls:ActionClusterView ClusterStyleResourceKey=\"M3ScreenHeaderActionCluster\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding ToggleSearchCommand}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionSemanticDescription=\"{Binding SearchButtonDescription}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("TargetType=\"controls:ActionClusterView\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("Property=\"PrimaryActionIconData\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding ShowSortActionsCommand}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("IsSecondaryActionVisible=\"{Binding IsSortButtonVisible}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionCommand=\"{Binding ShowViewActionsCommand}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("IsTertiaryActionVisible=\"{Binding IsViewButtonVisible}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<HorizontalStackLayout Style=\"{StaticResource M3ScreenHeaderActionCluster}\">", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Search}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Sort}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.ViewTiles}\"", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Trash_entry_actions_use_reusable_material_control()
        {
            string trashPage = LoadText(TrashPagePath);
            string trashEntryCardViewBase = LoadText(TrashEntryCardViewBasePath);
            string trashListEntryCardView = LoadText(TrashListEntryCardViewPath);
            string trashTileEntryCardView = LoadText(TrashTileEntryCardViewPath);

            Assert.Equal(1, CountOccurrences(trashPage, "<controls:TrashListEntryCardView"));
            Assert.Equal(1, CountOccurrences(trashPage, "<controls:TrashTileEntryCardView"));
            Assert.Equal(2, CountOccurrences(trashPage, "DeleteForeverCommand=\"{Binding BindingContext.DeleteForeverCommand, Source={x:Reference TrashRoot}}\""));
            Assert.Equal(2, CountOccurrences(trashPage, "RestoreCommand=\"{Binding BindingContext.RestoreCommand, Source={x:Reference TrashRoot}}\""));
            Assert.Contains("new ActionClusterView", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new ActionClusterView", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("actionCluster.PrimaryActionIconData = IconPathData.Delete", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("actionCluster.PrimaryActionCommand = DeleteForeverCommand", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("actionCluster.PrimaryActionIconButtonStyleResourceKey = \"M3DestructiveFileChromeIconButton\"", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("actionCluster.PrimaryActionSemanticDescription = $\"Delete {title} forever\"", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("actionCluster.SecondaryActionIconData = IconPathData.Reset", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("actionCluster.SecondaryActionCommand = RestoreCommand", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("actionCluster.SecondaryActionSemanticDescription = $\"Restore {title}\"", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.DoesNotContain("M3RowActionCluster", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionClusterView Grid.Row=\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Delete}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Reset}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,Auto\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,Auto,Auto\"", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_root_actions_use_reusable_material_control()
        {
            string syncSettingsPage = LoadText(SyncSettingsPagePath);
            string settingsInfoItemView = LoadText(SettingsInfoItemViewPath);

            Assert.Contains("<controls:SettingsInfoItemView LeadingIconData=\"{x:Static controls:IconPathData.Folder}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("AttentionLeadingIconData=\"{x:Static controls:IconPathData.Error}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsAttentionState=\"{Binding IsAttentionVisible}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("LeadingIconFrameStyleResourceKey=\"M3CardSyncThumbnailFrame\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("TitleTextStyleResourceKey=\"M3CardTitle\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailText=\"{Binding PathText}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryDetailText=\"{Binding DetailText}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding StatusText}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ActionClusterView ClusterStyleResourceKey=\"M3PanelActionCluster\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("ClusterStyleResourceKey=\"M3PanelActionCluster\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding BindingContext.RunRootCommand, Source={x:Reference SyncPageRoot}}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsPrimaryActionVisible=\"{Binding CanRunNow}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding BindingContext.PauseRootCommand, Source={x:Reference SyncPageRoot}}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsSecondaryActionVisible=\"{Binding CanPauseSync}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionCommand=\"{Binding BindingContext.ResumeRootCommand, Source={x:Reference SyncPageRoot}}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsTertiaryActionVisible=\"{Binding CanResumeSync}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("QuaternaryActionCommand=\"{Binding BindingContext.StopRootCommand, Source={x:Reference SyncPageRoot}}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("QuaternaryActionIconButtonStyleResourceKey=\"M3DestructiveFileChromeIconButton\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsQuaternaryActionVisible=\"{Binding CanStopSync}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("public class SettingsInfoItemView", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("AttentionLeadingIconDataProperty", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("AttentionTrailingTextStyleResourceKeyProperty", settingsInfoItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid ColumnDefinitions=\"Auto,*,Auto\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconFrame Grid.RowSpan", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"controls:IconFrame\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Grid.Column=\"2\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"Label\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionClusterView Grid.Row=\"3\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<HorizontalStackLayout Grid.Row=\"3\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding RunNowActionText}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding StopSyncActionText}\"", syncSettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Storage_bucket_rows_use_reusable_material_control()
        {
            string storagePage = LoadText(StoragePagePath);
            string storageBucketItemView = LoadText(StorageBucketItemViewPath);

            Assert.Equal(2, CountOccurrences(storagePage, "<controls:StorageBucketItemView"));
            Assert.Contains("PrimaryMetricText=\"{Binding SizeText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("SecondaryMetricText=\"{Binding CountText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("PrimaryMetricText=\"{Binding UsageText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("SecondaryMetricText=\"{Binding StatusText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Progress=\"{Binding UsageFraction}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("IsProgressVisible=\"True\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("public class StorageBucketItemView", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3SettingsListItemGrid\"", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultLeadingIconFrameStyleResourceKey = \"M3CardFileThumbnailFrame\"", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains("new LinearProgressView", storageBucketItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("x:DataType=\"services:CottonOnDeviceStorageBucketSnapshot\">\n                            <Grid", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("x:DataType=\"services:CottonStorageBudgetBucketSnapshot\">\n                            <Grid", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:LinearProgressView Grid.Row=\"2\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3CardFileThumbnailFrame}\"", storagePage, StringComparison.Ordinal);
        }

        [Fact]
        public void Storage_section_headers_use_reusable_material_control()
        {
            string storagePage = LoadText(StoragePagePath);
            string settingsSectionHeaderView = LoadText(SettingsSectionHeaderViewPath);

            Assert.Equal(4, CountOccurrences(storagePage, "<controls:SettingsSectionHeaderView"));
            Assert.Contains("Title=\"{Binding CloudQuotaTitle}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailText=\"{Binding CloudQuotaSummaryText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailTextStyleResourceKey=\"M3CardSupportingStrongLine\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("SecondaryDetailText=\"{Binding CloudQuotaDetailText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Progress=\"{Binding CloudQuotaUsageFraction}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("IsProgressVisible=\"{Binding IsCloudQuotaProgressVisible}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Title=\"Free up storage\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Title=\"Files on this device\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailText=\"{Binding OnDeviceSummaryText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Title=\"Temporary files\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailText=\"{Binding StorageBudgetSummaryText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("SecondaryDetailText=\"{Binding ProtectedOfflineText}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("public class SettingsSectionHeaderView", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3SettingsListItemGrid\"", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultLeadingIconFrameStyleResourceKey = \"M3CardUtilityThumbnailFrame\"", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("new LinearProgressView", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain("Grid.RowSpan=\"2\"\n                                        IconData=\"{x:Static controls:IconPathData.Cloud}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding CloudQuotaTitle}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"Free up storage\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding OnDeviceSummaryText}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding StorageBudgetSummaryText}\"", storagePage, StringComparison.Ordinal);
        }

        [Fact]
        public void Diagnostics_rows_use_reusable_material_control()
        {
            string diagnosticsPage = LoadText(DiagnosticsPagePath);
            string diagnosticsItemView = LoadText(DiagnosticsItemViewPath);

            Assert.Contains("<controls:SettingsSectionHeaderView Title=\"{Binding Title}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("IsTapEnabled=\"False\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DiagnosticsItemView LabelText=\"{Binding Label}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("ValueText=\"{Binding Value}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("public class DiagnosticsItemView", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3DiagnosticsItemGrid\"", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultLabelTextStyleResourceKey = \"M3CardSupporting\"", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultValueTextStyleResourceKey = \"M3CardSupportingPrimaryBlock\"", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("M3DiagnosticsLabelColumnWidth", diagnosticsItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Title}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Style=\"{StaticResource M3DiagnosticsItemGrid}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Label}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Grid.Column=\"1\"", diagnosticsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Linear_progress_indicators_use_reusable_material_control()
        {
            string transfersPage = LoadText(TransfersPagePath);
            string storagePage = LoadText(StoragePagePath);
            string linearProgressView = LoadText(LinearProgressViewPath);
            string metadataCardBodyView = LoadText(MetadataCardBodyViewPath);
            string settingsSectionHeaderView = LoadText(SettingsSectionHeaderViewPath);
            string storageBucketItemView = LoadText(StorageBucketItemViewPath);

            Assert.DoesNotContain("<controls:LinearProgressView", transfersPage, StringComparison.Ordinal);
            Assert.Contains("<controls:MetadataCardBodyView Progress=\"{Binding ProgressFraction}\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("Progress=\"{Binding ProgressFraction}\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("IsProgressVisible=\"{Binding IsProgressVisible}\"", transfersPage, StringComparison.Ordinal);
            Assert.Equal(0, CountOccurrences(storagePage, "<controls:LinearProgressView"));
            Assert.Contains("Progress=\"{Binding CloudQuotaUsageFraction}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("IsProgressVisible=\"{Binding IsCloudQuotaProgressVisible}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Progress=\"{Binding UsageFraction}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("new LinearProgressView", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("new LinearProgressView", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("new LinearProgressView", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultProgressStyleResourceKey = \"M3LinearProgressBar\"", linearProgressView, StringComparison.Ordinal);
            Assert.DoesNotContain("<ProgressBar", transfersPage + storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3LinearProgressBar}\"", transfersPage + storagePage, StringComparison.Ordinal);
        }

        [Fact]
        public void Document_viewer_headers_use_reusable_material_control()
        {
            string textViewerPage = LoadText(TextViewerPagePath);
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string documentViewerBodyView = LoadText(DocumentViewerBodyViewPath);
            string viewerInfoHeaderView = LoadText(ViewerInfoHeaderViewPath);

            Assert.Contains("<controls:DocumentViewerBodyView Grid.Row=\"1\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("GridStyleResourceKey=\"M3TextViewerContentGrid\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DocumentViewerBodyView Grid.Row=\"1\">", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerInfoHeaderView Details=\"{Binding Details}\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("Status=\"{Binding Status}\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisible=\"{Binding IsStatusVisible}\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerInfoHeaderView Details=\"{Binding Details}\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("Status=\"{Binding Status}\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisible=\"{Binding IsStatusVisible}\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("StackStyleResourceKey=\"M3PdfHeaderStack\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3ScreenHeaderTextStack\"", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultDetailsStyleResourceKey = \"M3CardSupportingLine\"", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultStatusStyleResourceKey = \"M3CardSupportingLine\"", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("public class DocumentViewerBodyView", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3DocumentViewerSurface\"", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto })", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star })", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _grid.Children", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("_grid.SetDynamicResource(StyleProperty, gridStyleResourceKey)", documentViewerBodyView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"\n              RowDefinitions=\"Auto,*\"", textViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"\n              RowDefinitions=\"Auto,*\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3ScreenHeaderTextStack}\">", textViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3PdfHeaderStack}\">", pdfViewerPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Text_viewer_body_uses_reusable_material_control()
        {
            string textViewerPage = LoadText(TextViewerPagePath);
            string textDocumentContentView = LoadText(TextDocumentContentViewPath);

            Assert.Contains("<controls:TextDocumentContentView Text=\"{Binding Content}\"", textViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Content}\"", textViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3TextViewerContent}\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("public class TextDocumentContentView", textDocumentContentView, StringComparison.Ordinal);
            Assert.Contains("DefaultTextStyleResourceKey = \"M3TextViewerContent\"", textDocumentContentView, StringComparison.Ordinal);
            Assert.Contains("new Label()", textDocumentContentView, StringComparison.Ordinal);
            Assert.Contains("ScrollView scrollView = new()", textDocumentContentView, StringComparison.Ordinal);
            Assert.Contains("_text.SetDynamicResource(StyleProperty, textStyleResourceKey)", textDocumentContentView, StringComparison.Ordinal);
            Assert.Contains("_text.Text = Text ?? string.Empty", textDocumentContentView, StringComparison.Ordinal);
        }

        [Fact]
        public void Dark_viewer_status_overlays_use_reusable_material_control()
        {
            string imageViewerPage = LoadText(ImageViewerPagePath);
            string mediaViewerPage = LoadText(MediaViewerPagePath);
            string darkViewerSurfaceView = LoadText(DarkViewerSurfaceViewPath);
            string viewerOverlayActionButtonView = LoadText(ViewerOverlayActionButtonViewPath);
            string viewerPlayOverlayView = LoadText(ViewerPlayOverlayViewPath);
            string viewerStatusOverlayView = LoadText(ViewerStatusOverlayViewPath);

            Assert.Contains("<controls:DarkViewerSurfaceView Grid.Row=\"1\">", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DarkViewerSurfaceView Grid.Row=\"1\">", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerStatusOverlayView Text=\"{Binding Status}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("StatusStyleResourceKey=\"M3ViewerOverlayStatusWithTrailingAction\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerOverlayActionButtonView x:Name=\"ResetButton\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding Source={x:Reference RootPage}, Path=ResetImageCommand}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"Reset image\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerStatusOverlayView Text=\"{Binding Status}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerPlayOverlayView x:Name=\"StartOverlay\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding Source={x:Reference RootPage}, Path=PlayMediaCommand}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"Play media\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("DefaultStatusStyleResourceKey = \"M3ViewerOverlayStatus\"", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("_status.SetDynamicResource(StyleProperty, statusStyleResourceKey)", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("public class ViewerOverlayActionButtonView", viewerOverlayActionButtonView, StringComparison.Ordinal);
            Assert.Contains("DefaultIconButtonStyleResourceKey = \"M3ViewerOverlayActionIconButton\"", viewerOverlayActionButtonView, StringComparison.Ordinal);
            Assert.Contains("_button.IconData = IconData ?? IconPathData.Reset", viewerOverlayActionButtonView, StringComparison.Ordinal);
            Assert.Contains("_button.Command = Command", viewerOverlayActionButtonView, StringComparison.Ordinal);
            Assert.Contains("public class ViewerPlayOverlayView", viewerPlayOverlayView, StringComparison.Ordinal);
            Assert.Contains("DefaultContainerStyleResourceKey = \"M3ViewerCenteredOverlay\"", viewerPlayOverlayView, StringComparison.Ordinal);
            Assert.Contains("DefaultIconButtonStyleResourceKey = \"M3ViewerCenteredPlayIconButton\"", viewerPlayOverlayView, StringComparison.Ordinal);
            Assert.Contains("IconData = IconPathData.Play", viewerPlayOverlayView, StringComparison.Ordinal);
            Assert.Contains("_playButton.Command = Command", viewerPlayOverlayView, StringComparison.Ordinal);
            Assert.Contains("public class DarkViewerSurfaceView", darkViewerSurfaceView, StringComparison.Ordinal);
            Assert.Contains("DefaultSurfaceStyleResourceKey = \"M3DarkViewerSurface\"", darkViewerSurfaceView, StringComparison.Ordinal);
            Assert.Contains("new Grid()", darkViewerSurfaceView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _surface.Children", darkViewerSurfaceView, StringComparison.Ordinal);
            Assert.Contains("_surface.SetDynamicResource(StyleProperty, surfaceStyleResourceKey)", darkViewerSurfaceView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Status}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Status}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton x:Name=\"ResetButton\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ViewerOverlayActionIconButton}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Reset image\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout x:Name=\"StartOverlay\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Play}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ViewerCenteredPlayIconButton}\"", mediaViewerPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Security_info_rows_use_reusable_material_control()
        {
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string settingsInfoItemView = LoadText(SettingsInfoItemViewPath);

            Assert.Equal(2, CountOccurrences(securitySettingsPage, "<controls:SettingsInfoItemView"));
            Assert.Contains("LeadingIconData=\"{x:Static controls:IconPathData.Check}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("AttentionLeadingIconData=\"{x:Static controls:IconPathData.Error}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsAttentionState=\"{Binding NeedsAttention}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailText=\"{Binding DetailText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailTextStyleResourceKey=\"M3CardSupportingBlock\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding StatusText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("LeadingIconData=\"{x:Static controls:IconPathData.Device}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("SecondaryDetailText=\"{Binding AccessText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("TertiaryDetailText=\"{Binding DurationText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("TrailingText=\"{Binding BadgeText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:SettingsSectionHeaderView IsVisible=\"{Binding IsAccountSessionsEmptyVisible}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"{Binding AccountSessionsEmptyTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailText=\"{Binding AccountSessionsEmptyDetails}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("TextStackStyleResourceKey=\"M3SettingsDenseStack\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("TitleTextStyleResourceKey=\"M3CardSupportingStrongLine\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryDetailTextStyleResourceKey=\"M3CardSupportingBlock\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("public class SettingsInfoItemView", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("IsAttentionStateProperty", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("AttentionLeadingIconFrameStyleResourceKeyProperty", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("AttentionTrailingTextStyleResourceKeyProperty", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("new ChipView", settingsInfoItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid ColumnDefinitions=\"Auto,*,Auto\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconFrame Grid.RowSpan", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"controls:IconFrame\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"controls:ChipView\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Grid.Column=\"1\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Grid.Row=\"1\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding AccountSessionsEmptyTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding AccountSessionsEmptyDetails}\"", securitySettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Settings_summary_headers_use_reusable_material_control()
        {
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string settingsSummaryHeaderView = LoadText(SettingsSummaryHeaderViewPath);

            Assert.Contains("<controls:SettingsSummaryHeaderView Title=\"{Binding PermissionTitle}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("StatusText=\"{Binding PermissionStatusText}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("DetailText=\"{Binding PermissionDetailText}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("StatusStyleResourceKey=\"M3CardTitle\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:SettingsSummaryHeaderView Title=\"Server push\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("StatusText=\"{Binding RemotePushStatusText}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisible=\"{Binding IsRemotePushStatusVisible}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsDetailVisible=\"False\"", notificationSettingsPage, StringComparison.Ordinal);

            Assert.Contains("<controls:SettingsSummaryHeaderView Title=\"{Binding DeviceUnlockTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("StatusText=\"{Binding DeviceUnlockStatusText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("DetailText=\"{Binding DeviceUnlockDetailText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:SettingsSummaryHeaderView Title=\"{Binding PermissionLedgerTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("StatusText=\"{Binding PermissionLedgerStatusText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("DetailText=\"{Binding PermissionLedgerDetailText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:SettingsSummaryHeaderView Title=\"{Binding AccountSessionsTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("StatusText=\"{Binding AccountSessionsStatusText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("DetailText=\"{Binding AccountSessionsDetailText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3SettingsSummaryGrid\"", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultTitleStyleResourceKey = \"M3CardTitle\"", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultStatusStyleResourceKey = \"M3CardSupportingLine\"", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultDetailStyleResourceKey = \"M3CardSupportingBlock\"", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding PermissionTitle}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:SettingsSummaryHeaderView Title=\"{Binding AppLockTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding AppLockTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding DeviceUnlockTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding PermissionLedgerTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding AccountSessionsTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:SettingsSummaryHeaderView Title=\"{Binding LogoutCacheCleanupTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding LogoutCacheCleanupTitle}\"", securitySettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Settings_cards_use_reusable_material_shell()
        {
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string settingsCardView = LoadText(SettingsCardViewPath);
            string storagePage = LoadText(StoragePagePath);
            string backupSetupPage = LoadText(BackupSetupPagePath);

            Assert.Equal(2, CountOccurrences(notificationSettingsPage, "<controls:SettingsCardView"));
            Assert.Equal(5, CountOccurrences(securitySettingsPage, "<controls:SettingsCardView"));
            Assert.Equal(4, CountOccurrences(storagePage, "<controls:SettingsCardView"));
            Assert.Equal(3, CountOccurrences(backupSetupPage, "<controls:SettingsCardView"));
            Assert.Contains("Title=\"{Binding PermissionTitle}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding AppLockTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("Title=\"Free up storage\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Progress=\"{Binding CloudQuotaUsageFraction}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("Text=\"Camera backup\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("TapCommand=\"{Binding ChooseDestinationCommand}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("DefaultCardStyleResourceKey = \"M3ContentCard\"", settingsCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3SettingsSectionStack\"", settingsCardView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _stack.Children", settingsCardView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Style=\"{StaticResource M3ContentCard}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Style=\"{StaticResource M3ContentCard}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Style=\"{StaticResource M3ContentCard}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Style=\"{StaticResource M3ContentCard}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3SettingsSectionStack}\">\n                    <controls:SettingsSummaryHeaderView", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3SettingsSectionStack}\">\n                    <controls:SettingsSummaryHeaderView", securitySettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Settings_toggle_rows_use_reusable_material_control()
        {
            string backupSetupPage = LoadText(BackupSetupPagePath);
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string settingsToggleItemView = LoadText(SettingsToggleItemViewPath);
            string styles = LoadText(StylesResourcePath);

            Assert.Equal(5, CountOccurrences(backupSetupPage, "<controls:SettingsToggleItemView"));
            Assert.Equal(1, CountOccurrences(notificationSettingsPage, "<controls:SettingsToggleItemView"));
            Assert.Equal(2, CountOccurrences(securitySettingsPage, "<controls:SettingsToggleItemView"));
            Assert.Contains("Text=\"Camera backup\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanEnableBackup}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding IsBackupEnabled, Mode=OneWay}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"Photos only\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding PhotosOnly, Mode=TwoWay}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"Require charging\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding ChargingOnly, Mode=TwoWay}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"Wi-Fi only\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding WifiOnly, Mode=TwoWay}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"Cellular uploads\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding AllowCellular, Mode=TwoWay}\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding Title}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("SupportingText=\"{Binding DetailText}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsLeadingIconVisible=\"False\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanToggle}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding IsEnabled, Mode=TwoWay}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding AppLockTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("SupportingText=\"{Binding AppLockStatusText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("DetailText=\"{Binding AppLockDetailText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanToggleAppLock}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding IsAppLockEnabled, Mode=TwoWay}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding LogoutCacheCleanupTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("SupportingText=\"{Binding LogoutCacheCleanupStatusText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("DetailText=\"{Binding LogoutCacheCleanupDetailText}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanToggleLogoutCacheCleanup}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsToggled=\"{Binding IsLogoutCacheCleanupEnabled, Mode=TwoWay}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3SettingsListItemGrid\"", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultTextStackStyleResourceKey = \"M3SettingsDenseStack\"", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultSwitchStyleResourceKey = \"M3Switch\"", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("DetailTextProperty", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultDetailTextStyleResourceKey = \"M3CardSupportingBlock\"", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("new Binding(nameof(IsToggled), source: this, mode: BindingMode.TwoWay)", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("_toggleSwitch.SetDynamicResource(StyleProperty, switchStyleResourceKey)", settingsToggleItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ToggleSwitch", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ToggleSwitch", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ToggleSwitch", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3SettingsToggleGroupGrid", backupSetupPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3SettingsToggleGroupGrid", styles, StringComparison.Ordinal);
            Assert.DoesNotContain("Grid.RowSpan=\"3\"", backupSetupPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_content_cards_use_reusable_material_shell()
        {
            string recentFilesPage = LoadText(RecentFilesPagePath);
            string activityFeedPage = LoadText(ActivityFeedPagePath);
            string captureDestinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string diagnosticsPage = LoadText(DiagnosticsPagePath);
            string mainPage = LoadText(MainPagePath);
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string syncSettingsPage = LoadText(SyncSettingsPagePath);
            string textViewerPage = LoadText(TextViewerPagePath);
            string trashPage = LoadText(TrashPagePath);
            string contentCardView = LoadText(ContentCardViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string settingsActionHeaderCardView = LoadText(SettingsActionHeaderCardViewPath);
            string trashListEntryCardView = LoadText(TrashListEntryCardViewPath);
            string trashTileEntryCardView = LoadText(TrashTileEntryCardViewPath);

            Assert.Contains("public class ContentCardView", contentCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultCardStyleResourceKey = \"M3ContentCard\"", contentCardView, StringComparison.Ordinal);
            Assert.Contains("_card.Content = BodyContent", contentCardView, StringComparison.Ordinal);
            Assert.Equal(1, CountOccurrences(recentFilesPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(activityFeedPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(captureDestinationPickerPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(diagnosticsPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(mainPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(pdfViewerPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(syncSettingsPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(textViewerPage, "<controls:ContentCardView"));
            Assert.DoesNotContain("<controls:ContentCardView", trashPage, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey=\"M3AuthPanel\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey = \"M3FileTileCard\"", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey=\"M3PdfPageSurface\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey=\"M3TextViewerSurface\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey = \"M3SelectableContentCard\"", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey = \"M3SelectableTrashTileCard\"", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("Text=\"Load more\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding DisplayName}\"", captureDestinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding BindingContext.RunRootCommand, Source={x:Reference SyncPageRoot}}\"", syncSettingsPage, StringComparison.Ordinal);

            string[] pages =
            [
                recentFilesPage,
                activityFeedPage,
                captureDestinationPickerPage,
                diagnosticsPage,
                mainPage,
                pdfViewerPage,
                syncSettingsPage,
                textViewerPage,
                trashPage,
            ];

            foreach (string page in pages)
            {
                Assert.DoesNotContain("<Border Style=\"{StaticResource M3ContentCard}\"", page, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("<Border IsVisible=\"{Binding Display.IsSignInVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthPanel}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileCard}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3PdfPageSurface}\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3TextViewerSurface}\"", textViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Style=\"{StaticResource M3SelectableContentCard}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Style=\"{StaticResource M3SelectableTrashTileCard}\"", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_floating_action_button_uses_reusable_material_control()
        {
            string floatingActionButtonView = LoadText(FloatingActionButtonViewPath);
            string mainPage = LoadText(MainPagePath);

            Assert.Contains("<controls:FloatingActionButtonView Grid.Row=\"1\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IconData=\"{x:Static controls:IconPathData.Plus}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding ShowFileAddActionsCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding Display.IsFileAddButtonVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding Display.IsFileBrowserChromeEnabled}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"Add files\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class FloatingActionButtonView", floatingActionButtonView, StringComparison.Ordinal);
            Assert.Contains("DefaultIconButtonStyleResourceKey = \"M3FloatingActionIconButton\"", floatingActionButtonView, StringComparison.Ordinal);
            Assert.Contains("new IconButton()", floatingActionButtonView, StringComparison.Ordinal);
            Assert.Contains("_button.IconData = IconData ?? IconPathData.Plus", floatingActionButtonView, StringComparison.Ordinal);
            Assert.Contains("_button.IsEnabled = IsEnabled", floatingActionButtonView, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton Grid.Row=\"1\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FloatingActionIconButton}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"Add files\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_entry_actions_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);

            Assert.Equal(0, CountOccurrences(mainPage, "<controls:FileEntryActionButtonView"));
            Assert.Equal(2, CountOccurrences(mainPage, "EntryActionsCommand=\"{Binding BindingContext.ShowFileBrowserEntryActionsCommand, Source={x:Reference RootPage}}\""));
            Assert.Contains("CommandParameter=\"{Binding .}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsActionEnabled=\"{Binding BindingContext.Display.IsFileBrowserChromeEnabled, Source={x:Reference RootPage}}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsActionVisible=\"{Binding BindingContext.Display.IsFileEntryActionsVisible, Source={x:Reference RootPage}}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IconButtonStyleResourceKey = \"M3FileTileActionIconButton\"", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("ActionSemanticDescription=\"{Binding Name, StringFormat='Actions for {0}'}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("new FileEntryActionButtonView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("new FileEntryActionButtonView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_actionButton.Command = EntryActionsCommand", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("_actionButton.Command = EntryActionsCommand", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Name, StringFormat='Actions for {0}'}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileActionIconButton}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void File_entry_metadata_blocks_use_reusable_material_controls()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string fileListMetadataView = LoadText(FileListMetadataViewPath);
            string fileTileMetadataView = LoadText(FileTileMetadataViewPath);
            string trashListEntryCardView = LoadText(TrashListEntryCardViewPath);
            string trashTileEntryCardView = LoadText(TrashTileEntryCardViewPath);

            Assert.Equal(0, CountOccurrences(mainPage, "<controls:FileListMetadataView"));
            Assert.Contains("Title=\"{Binding Name}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Detail=\"{Binding DisplayDetails}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("new FileListMetadataView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("_metadata.Title = Title ?? string.Empty", fileListEntryRowView, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:FileTileMetadataView", mainPage, StringComparison.Ordinal);
            Assert.Contains("new FileTileMetadataView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("_metadata.Title = Title ?? string.Empty", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileListTextStack}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:FileEntryTextView", mainPage, StringComparison.Ordinal);

            Assert.DoesNotContain("<controls:FileListMetadataView", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:FileTileMetadataView", trashPage, StringComparison.Ordinal);
            Assert.Contains("new FileListMetadataView", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new FileTileMetadataView", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("IsTrailingTextVisible = true", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3FileListMetadataGrid\"", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultTitleStyleResourceKey = \"M3CardTitle\"", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultDetailStyleResourceKey = \"M3CardSupportingLine\"", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3FileTileTextStack\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3CardTextStack}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileTextStack}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:FileEntryTextView", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_screen_content_grids_use_reusable_material_shell()
        {
            string fileVersionHistoryPage = LoadText(FileVersionHistoryPagePath);
            string recentFilesPage = LoadText(RecentFilesPagePath);
            string screenContentGridView = LoadText(ScreenContentGridViewPath);
            string trashPage = LoadText(TrashPagePath);

            Assert.Contains("<controls:ScreenContentGridView Grid.Row=\"1\">", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenContentGridView Grid.Row=\"1\">", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenContentGridView Grid.Row=\"1\"\n                                        ExtraAutoRows=\"2\">", trashPage, StringComparison.Ordinal);
            Assert.Contains("public class ScreenContentGridView", screenContentGridView, StringComparison.Ordinal);
            Assert.Contains("new Grid", screenContentGridView, StringComparison.Ordinal);
            Assert.Contains("new RowDefinition { Height = GridLength.Auto }", screenContentGridView, StringComparison.Ordinal);
            Assert.Contains("new RowDefinition { Height = GridLength.Star }", screenContentGridView, StringComparison.Ordinal);
            Assert.Contains("ExtraAutoRowsProperty", screenContentGridView, StringComparison.Ordinal);
            Assert.Contains("int extraAutoRows = Math.Max(0, ExtraAutoRows)", screenContentGridView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3ScreenContentGrid\"", screenContentGridView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _grid.Children", screenContentGridView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"\n              RowDefinitions=\"Auto,Auto,*\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"\n              RowDefinitions=\"Auto,Auto,*\"", recentFilesPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"\n              RowDefinitions=\"Auto,Auto,Auto,Auto,*\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ScreenContentGrid}\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ScreenContentGrid}\"", recentFilesPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ScreenContentGrid}\"", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_screen_scroll_bodies_use_reusable_material_shell()
        {
            string screenScrollBodyView = LoadText(ScreenScrollBodyViewPath);

            string[] scrollBodyScreenPaths =
            [
                ActivityFeedPagePath,
                BackupSetupPagePath,
                CaptureDestinationPickerPagePath,
                CaptureInboxPagePath,
                DiagnosticsPagePath,
                NotificationSettingsPagePath,
                SecuritySettingsPagePath,
                StoragePagePath,
                SyncSettingsPagePath,
                TransfersPagePath,
            ];

            Assert.Contains("public class ScreenScrollBodyView", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("new ScrollView", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("new VerticalStackLayout", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3ScreenContentStack\"", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _stack.Children", screenScrollBodyView, StringComparison.Ordinal);

            foreach (string screenPath in scrollBodyScreenPaths)
            {
                string page = LoadText(screenPath);

                Assert.Contains("<controls:ScreenScrollBodyView Grid.Row=\"1\">", page, StringComparison.Ordinal);
                Assert.DoesNotContain("<ScrollView Grid.Row=\"1\">", page, StringComparison.Ordinal);
                Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3ScreenContentStack}\">", page, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Secondary_screen_headers_use_reusable_material_control()
        {
            string[] screenPaths =
            [
                RecentFilesPagePath,
                ActivityFeedPagePath,
                TransfersPagePath,
                FileVersionHistoryPagePath,
                CaptureInboxPagePath,
                CaptureDestinationPickerPagePath,
                TrashPagePath,
                DiagnosticsPagePath,
                SyncSettingsPagePath,
                NotificationSettingsPagePath,
                SecuritySettingsPagePath,
                BackupSetupPagePath,
                StoragePagePath,
            ];

            foreach (string screenPath in screenPaths)
            {
                string page = LoadText(screenPath);

                Assert.Contains("<controls:ScreenHeaderView", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3ScreenHeaderGrid", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3ScreenHeaderActivityIndicator", page, StringComparison.Ordinal);
            }

            string syncSettingsPage = LoadText(SyncSettingsPagePath);
            string backupSetupPage = LoadText(BackupSetupPagePath);
            string destinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string fileVersionHistoryPage = LoadText(FileVersionHistoryPagePath);
            string storagePage = LoadText(StoragePagePath);
            string trashPage = LoadText(TrashPagePath);

            Assert.Contains("IsSupportingTextVisible=\"{Binding IsSummaryVisible}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("IsSupportingTextMultiline=\"True\"", backupSetupPage, StringComparison.Ordinal);
            Assert.Contains("IsSupportingTextVisible=\"{Binding IsPathTextVisible}\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("IsSupportingTextMultiline=\"True\"", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("SupportingTextStyleResourceKey=\"M3CardTitle\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("DetailTextStyleResourceKey=\"M3ScreenHeaderSupportingMultiline\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("TitleStyleResourceKey=\"M3ScreenMetric\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("DetailTextStyleResourceKey=\"M3CardSupportingLine\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenHeaderView Title=\"Trash\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenHeaderView.ActionContent>", trashPage, StringComparison.Ordinal);
            Assert.Contains("IsBusy=\"{Binding IsBusy}\"", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_screen_status_rows_use_reusable_material_control()
        {
            string[] screenPaths =
            [
                RecentFilesPagePath,
                ActivityFeedPagePath,
                TransfersPagePath,
                FileVersionHistoryPagePath,
                CaptureInboxPagePath,
                CaptureDestinationPickerPagePath,
                TrashPagePath,
                DiagnosticsPagePath,
                SyncSettingsPagePath,
                NotificationSettingsPagePath,
                SecuritySettingsPagePath,
                BackupSetupPagePath,
                StoragePagePath,
            ];

            foreach (string screenPath in screenPaths)
            {
                string page = LoadText(screenPath);

                Assert.Contains("<controls:ScreenStatusView", page, StringComparison.Ordinal);
                Assert.DoesNotContain("M3ScreenStatus", page, StringComparison.Ordinal);
            }

            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);

            Assert.Contains("IsVisible=\"{Binding IsNeutralStatusVisible}\"", notificationSettingsPage, StringComparison.Ordinal);
        }

        private static XDocument LoadResourceDictionary(string relativePath)
        {
            string repositoryRoot = FindRepositoryRoot(relativePath);
            string resourcePath = Path.Combine(repositoryRoot, relativePath);
            return XDocument.Load(resourcePath);
        }

        private static string LoadText(string relativePath)
        {
            string repositoryRoot = FindRepositoryRoot(relativePath);
            string resourcePath = Path.Combine(repositoryRoot, relativePath);
            return File.ReadAllText(resourcePath);
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int startIndex = 0;
            while (startIndex < text.Length)
            {
                int matchIndex = text.IndexOf(value, startIndex, StringComparison.Ordinal);
                if (matchIndex < 0)
                {
                    return count;
                }

                count++;
                startIndex = matchIndex + value.Length;
            }

            return count;
        }

        private static string FindRepositoryRoot(string relativePath)
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException($"Could not find repository root containing {relativePath}.");
        }

        private static double GetDoubleResource(XDocument document, string key)
        {
            XElement element = document.Descendants()
                .Single(descendant => string.Equals(
                    (string?)descendant.Attribute(XamlNamespace + "Key"),
                    key,
                    StringComparison.Ordinal));
            return double.Parse(element.Value, CultureInfo.InvariantCulture);
        }

        private static int GetIntResource(XDocument document, string key)
        {
            XElement element = document.Descendants()
                .Single(descendant => string.Equals(
                    (string?)descendant.Attribute(XamlNamespace + "Key"),
                    key,
                    StringComparison.Ordinal));
            return int.Parse(element.Value, CultureInfo.InvariantCulture);
        }

        private static IReadOnlyDictionary<string, string> GetStyleSetters(XDocument document, string styleKey)
        {
            XElement style = GetStyleByKey(document, styleKey);

            return style.Elements()
                .Where(element => string.Equals(element.Name.LocalName, "Setter", StringComparison.Ordinal))
                .ToDictionary(
                    element => (string)element.Attribute("Property")!,
                    element => (string)element.Attribute("Value")!,
                    StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetControlDarkDefaultProperties()
        {
            string repositoryRoot = FindRepositoryRoot(StylesResourcePath);
            string controlsPath = Path.Combine(repositoryRoot, ControlsDirectoryPath);
            Dictionary<string, IReadOnlyCollection<string>> result = new(StringComparer.Ordinal);

            foreach (string filePath in Directory.EnumerateFiles(controlsPath, "*.cs").OrderBy(path => path, StringComparer.Ordinal))
            {
                string controlName = Path.GetFileNameWithoutExtension(filePath);
                string source = File.ReadAllText(filePath);
                string[] bindablePropertyBlocks = source.Split(
                    "public static readonly BindableProperty",
                    StringSplitOptions.RemoveEmptyEntries);
                SortedSet<string> propertyNames = new(StringComparer.Ordinal);

                foreach (string block in bindablePropertyBlocks)
                {
                    if (!block.Contains("MaterialResources.Get<Color>(\"M3Dark", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Match match = Regex.Match(
                        block,
                        "nameof\\((?<property>[A-Za-z_][A-Za-z0-9_]*)\\)",
                        RegexOptions.CultureInvariant);

                    if (match.Success)
                    {
                        propertyNames.Add(match.Groups["property"].Value);
                    }
                }

                if (propertyNames.Count > 0)
                {
                    result[controlName] = propertyNames.ToArray();
                }
            }

            return result;
        }

        private static IReadOnlyDictionary<string, string> GetImplicitStyleSetters(XDocument document, string targetType)
        {
            XElement style = document.Descendants()
                .Single(descendant => string.Equals(
                    descendant.Name.LocalName,
                    "Style",
                    StringComparison.Ordinal)
                    && string.Equals(
                        (string?)descendant.Attribute("TargetType"),
                        targetType,
                        StringComparison.Ordinal)
                    && descendant.Attribute(XamlNamespace + "Key") is null);

            return GetStyleSettersIncludingBasedOn(document, style);
        }

        private static IReadOnlyDictionary<string, string> GetStyleSettersIncludingBasedOn(XDocument document, XElement style)
        {
            Dictionary<string, string> setters = new(StringComparer.Ordinal);
            string? basedOn = (string?)style.Attribute("BasedOn");

            if (basedOn is not null)
            {
                XElement baseStyle = GetStyleByKey(document, GetStaticResourceKey(basedOn));
                IReadOnlyDictionary<string, string> baseSetters = GetStyleSettersIncludingBasedOn(document, baseStyle);

                foreach (KeyValuePair<string, string> setter in baseSetters)
                {
                    setters[setter.Key] = setter.Value;
                }
            }

            foreach (XElement setter in style.Elements().Where(element => string.Equals(element.Name.LocalName, "Setter", StringComparison.Ordinal)))
            {
                XAttribute? propertyAttribute = setter.Attribute("Property");
                XAttribute? valueAttribute = setter.Attribute("Value");

                if (propertyAttribute is not null && valueAttribute is not null)
                {
                    setters[propertyAttribute.Value] = valueAttribute.Value;
                }
            }

            return setters;
        }

        private static XElement GetStyleByKey(XDocument document, string styleKey)
        {
            return document.Descendants()
                .Single(descendant => string.Equals(
                    descendant.Name.LocalName,
                    "Style",
                    StringComparison.Ordinal)
                    && string.Equals(
                        (string?)descendant.Attribute(XamlNamespace + "Key"),
                        styleKey,
                        StringComparison.Ordinal));
        }

        private static string GetStaticResourceKey(string value)
        {
            const string prefix = "{StaticResource ";
            const string suffix = "}";

            if (!value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal))
            {
                throw new FormatException($"Style BasedOn value '{value}' is not a StaticResource reference.");
            }

            return value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
        }
    }
}
