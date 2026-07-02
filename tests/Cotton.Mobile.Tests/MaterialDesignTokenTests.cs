using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MaterialDesignTokenTests
    {
        private const string SpacingResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MSpacing.xaml";
        private const string ColorsResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MColors.xaml";
        private const string TypeResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MType.xaml";
        private const string InteractionResourcePath = "src/Cotton.Mobile/Resources/Styles/Theme/MInteraction.xaml";
        private const string StylesResourcePath = "src/Cotton.Mobile/Resources/Styles/Styles.xaml";
        private const string ControlsDirectoryPath = "src/Cotton.Mobile/Controls";
        private const string MainActivityPath = "src/Cotton.Mobile/Platforms/Android/MainActivity.cs";
        private const string AndroidLightColorsPath = "src/Cotton.Mobile/Platforms/Android/Resources/values/colors.xml";
        private const string AndroidDarkColorsPath = "src/Cotton.Mobile/Platforms/Android/Resources/values-night/colors.xml";
        private const string AndroidLightStylesPath = "src/Cotton.Mobile/Platforms/Android/Resources/values/styles.xml";
        private const string AndroidDarkStylesPath = "src/Cotton.Mobile/Platforms/Android/Resources/values-night/styles.xml";
        private const string MainPagePath = "src/Cotton.Mobile/MainPage.xaml";
        private const string TrashPagePath = "src/Cotton.Mobile/TrashPage.xaml";
        private const string MaterialDialogPagePath = "src/Cotton.Mobile/Controls/MaterialDialogPage.cs";
        private const string MaterialActionSheetPagePath = "src/Cotton.Mobile/Controls/MaterialActionSheetPage.cs";
        private const string AppLockGatePagePath = "src/Cotton.Mobile/AppLockGatePage.xaml";
        private const string RecentFilesPagePath = "src/Cotton.Mobile/RecentFilesPage.xaml";
        private const string ActivityFeedPagePath = "src/Cotton.Mobile/ActivityFeedPage.xaml";
        private const string TransfersPagePath = "src/Cotton.Mobile/TransfersPage.xaml";
        private const string FileVersionHistoryPagePath = "src/Cotton.Mobile/FileVersionHistoryPage.xaml";
        private const string CaptureInboxPagePath = "src/Cotton.Mobile/CaptureInboxPage.xaml";
        private const string CaptureDestinationPickerPagePath = "src/Cotton.Mobile/CaptureDestinationPickerPage.xaml";
        private const string TextViewerPagePath = "src/Cotton.Mobile/TextViewerPage.xaml";
        private const string TextViewerPageCodeBehindPath = "src/Cotton.Mobile/TextViewerPage.xaml.cs";
        private const string ImageViewerPagePath = "src/Cotton.Mobile/ImageViewerPage.xaml";
        private const string ImageViewerPageCodeBehindPath = "src/Cotton.Mobile/ImageViewerPage.xaml.cs";
        private const string MediaViewerPagePath = "src/Cotton.Mobile/MediaViewerPage.xaml";
        private const string MediaViewerPageCodeBehindPath = "src/Cotton.Mobile/MediaViewerPage.xaml.cs";
        private const string PdfViewerPagePath = "src/Cotton.Mobile/PdfViewerPage.xaml";
        private const string PdfViewerPageCodeBehindPath = "src/Cotton.Mobile/PdfViewerPage.xaml.cs";
        private const string PdfPreviewPageViewPath = "src/Cotton.Mobile/Controls/PdfPreviewPageView.cs";
        private const string DiagnosticsPagePath = "src/Cotton.Mobile/DiagnosticsPage.xaml";
        private const string SyncSettingsPagePath = "src/Cotton.Mobile/SyncSettingsPage.xaml";
        private const string NotificationSettingsPagePath = "src/Cotton.Mobile/NotificationSettingsPage.xaml";
        private const string SecuritySettingsPagePath = "src/Cotton.Mobile/SecuritySettingsPage.xaml";
        private const string BackupSetupPagePath = "src/Cotton.Mobile/BackupSetupPage.xaml";
        private const string StoragePagePath = "src/Cotton.Mobile/StoragePage.xaml";
        private const string AuthLegalFooterViewPath = "src/Cotton.Mobile/Controls/AuthLegalFooterView.cs";
        private const string AuthSignInPanelViewPath = "src/Cotton.Mobile/Controls/AuthSignInPanelView.cs";
        private const string FocusedInputChromeBehaviorPath = "src/Cotton.Mobile/Behaviors/FocusedInputChromeBehavior.cs";
        private const string BrandHeaderViewPath = "src/Cotton.Mobile/Controls/BrandHeaderView.cs";
        private const string BrandMarkViewPath = "src/Cotton.Mobile/Controls/BrandMarkView.cs";
        private const string LongPressBehaviorPath = "src/Cotton.Mobile/Behaviors/LongPressBehavior.cs";
        private const string TouchSurfaceViewPath = "src/Cotton.Mobile/Controls/TouchSurfaceView.cs";
        private const string CenteredGateViewPath = "src/Cotton.Mobile/Controls/CenteredGateView.cs";
        private const string EmptyStateViewPath = "src/Cotton.Mobile/Controls/EmptyStateView.cs";
        private const string ActionListItemViewPath = "src/Cotton.Mobile/Controls/ActionListItemView.cs";
        private const string MainPageRootViewPath = "src/Cotton.Mobile/Controls/MainPageRootView.cs";
        private const string FileListSkeletonViewPath = "src/Cotton.Mobile/Controls/FileListSkeletonView.cs";
        private const string MetadataListSkeletonViewPath = "src/Cotton.Mobile/Controls/MetadataListSkeletonView.cs";
        private const string FileEntryTextViewPath = "src/Cotton.Mobile/Controls/FileEntryTextView.cs";
        private const string FileListMetadataViewPath = "src/Cotton.Mobile/Controls/FileListMetadataView.cs";
        private const string FileListEntryRowViewPath = "src/Cotton.Mobile/Controls/FileListEntryRowView.cs";
        private const string FileBrowserTopBarViewPath = "src/Cotton.Mobile/Controls/FileBrowserTopBarView.cs";
        private const string FileStatusActionViewPath = "src/Cotton.Mobile/Controls/FileStatusActionView.cs";
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
        private const string DarkViewerPagePath = "src/Cotton.Mobile/Controls/DarkViewerPage.cs";
        private const string DocumentViewerPagePath = "src/Cotton.Mobile/Controls/DocumentViewerPage.cs";
        private const string TextDocumentContentViewPath = "src/Cotton.Mobile/Controls/TextDocumentContentView.cs";
        private const string DocumentViewerBodyViewPath = "src/Cotton.Mobile/Controls/DocumentViewerBodyView.cs";
        private const string ViewerImageViewPath = "src/Cotton.Mobile/Controls/ViewerImageView.cs";
        private const string ViewerMediaElementViewPath = "src/Cotton.Mobile/Controls/ViewerMediaElementView.cs";
        private const string TrashEntryCardViewBasePath = "src/Cotton.Mobile/Controls/TrashEntryCardViewBase.cs";
        private const string TrashListEntryCardViewPath = "src/Cotton.Mobile/Controls/TrashListEntryCardView.cs";
        private const string TrashTileEntryCardViewPath = "src/Cotton.Mobile/Controls/TrashTileEntryCardView.cs";
        private const string LoadingStatusViewPath = "src/Cotton.Mobile/Controls/LoadingStatusView.cs";
        private const string LayeredContentViewPath = "src/Cotton.Mobile/Controls/LayeredContentView.cs";
        private const string AttentionStatusViewPath = "src/Cotton.Mobile/Controls/AttentionStatusView.cs";
        private const string MaterialRefreshViewPath = "src/Cotton.Mobile/Controls/MaterialRefreshView.cs";
        private const string ScreenContentGridViewPath = "src/Cotton.Mobile/Controls/ScreenContentGridView.cs";
        private const string ScreenHeaderViewPath = "src/Cotton.Mobile/Controls/ScreenHeaderView.cs";
        private const string ScreenShellViewPath = "src/Cotton.Mobile/Controls/ScreenShellView.cs";
        private const string ScreenScrollBodyViewPath = "src/Cotton.Mobile/Controls/ScreenScrollBodyView.cs";
        private const string MaterialCollectionViewPath = "src/Cotton.Mobile/Controls/MaterialCollectionView.cs";
        private const string StackedContentViewPath = "src/Cotton.Mobile/Controls/StackedContentView.cs";
        private const string StackedItemsViewPath = "src/Cotton.Mobile/Controls/StackedItemsView.cs";
        private const string ScreenStatusViewPath = "src/Cotton.Mobile/Controls/ScreenStatusView.cs";
        private const string FileBrowserNavigationBarViewPath = "src/Cotton.Mobile/Controls/FileBrowserNavigationBarView.cs";
        private const string NavigationBarViewPath = "src/Cotton.Mobile/Controls/NavigationBarView.cs";
        private const string NoticePanelViewPath = "src/Cotton.Mobile/Controls/NoticePanelView.cs";
        private const string LinearProgressViewPath = "src/Cotton.Mobile/Controls/LinearProgressView.cs";
        private const string SelectionBarViewPath = "src/Cotton.Mobile/Controls/SelectionBarView.cs";
        private const string SelectionOverlayViewPath = "src/Cotton.Mobile/Controls/SelectionOverlayView.cs";
        private const string TopAppBarPath = "src/Cotton.Mobile/Controls/TopAppBar.xaml";
        private const string TopAppBarCodeBehindPath = "src/Cotton.Mobile/Controls/TopAppBar.xaml.cs";
        private const string TopAppBarContentGridViewPath = "src/Cotton.Mobile/Controls/TopAppBarContentGridView.cs";
        private const string TopAppBarTitleLabelPath = "src/Cotton.Mobile/Controls/TopAppBarTitleLabel.cs";
        private const string ViewerInfoHeaderViewPath = "src/Cotton.Mobile/Controls/ViewerInfoHeaderView.cs";
        private const string DarkViewerSurfaceViewPath = "src/Cotton.Mobile/Controls/DarkViewerSurfaceView.cs";
        private const string ViewerStatusOverlayViewPath = "src/Cotton.Mobile/Controls/ViewerStatusOverlayView.cs";
        private const string ViewerPlayOverlayViewPath = "src/Cotton.Mobile/Controls/ViewerPlayOverlayView.cs";
        private const string ViewerOverlayActionButtonViewPath = "src/Cotton.Mobile/Controls/ViewerOverlayActionButtonView.cs";
        private const string WrappedItemsViewPath = "src/Cotton.Mobile/Controls/WrappedItemsView.cs";
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
        public void Material_switch_animates_state_and_thumb_motion()
        {
            string toggleSwitch = LoadText(Path.Combine(ControlsDirectoryPath, "ToggleSwitch.cs"));
            string interaction = LoadText(InteractionResourcePath);

            Assert.Contains("TrackColorAnimationName = \"M3SwitchTrackColor\"", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("TrackBorderColorAnimationName = \"M3SwitchTrackBorderColor\"", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("ThumbColorAnimationName = \"M3SwitchThumbColor\"", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("ThumbTranslationAnimationName = \"M3SwitchThumbTranslation\"", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("OpacityAnimationName = \"M3SwitchOpacity\"", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("propertyChanged: OnToggledPropertyChanged", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("UpdateVisualState(animateState: true, animateThumbTranslation: true)", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateBackgroundColor(", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateColor(", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("ResolveThumbTranslation", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("M3MotionSelectionDuration", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionSelectionDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_track.BackgroundColor = ResolveTrackColor()", toggleSwitch, StringComparison.Ordinal);
            Assert.DoesNotContain("_thumb.BackgroundColor = ResolveThumbColor()", toggleSwitch, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_thumb.HorizontalOptions = IsToggled ? LayoutOptions.End : LayoutOptions.Start",
                toggleSwitch,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Material_icon_primitives_animate_color_state()
        {
            string iconView = LoadText(Path.Combine(ControlsDirectoryPath, "IconView.cs"));
            string iconFrame = LoadText(Path.Combine(ControlsDirectoryPath, "IconFrame.cs"));
            string interaction = LoadText(InteractionResourcePath);

            Assert.Contains("IconColorAnimationName = \"M3IconViewColor\"", iconView, StringComparison.Ordinal);
            Assert.Contains("FrameBackgroundAnimationName = \"M3IconFrameBackground\"", iconFrame, StringComparison.Ordinal);
            Assert.Contains("BorderColorAnimationName = \"M3IconFrameBorderColor\"", iconFrame, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateColor(", iconView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateBackgroundColor(", iconFrame, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateColor(", iconFrame, StringComparison.Ordinal);
            Assert.Contains("ResolveCurrentIconColor", iconView, StringComparison.Ordinal);
            Assert.Contains("ResolveCurrentBorderColor", iconFrame, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", iconView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", iconFrame, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_path.Fill = new SolidColorBrush(IconColor);", iconView, StringComparison.Ordinal);
            Assert.DoesNotContain("_container.BackgroundColor = FrameBackgroundColor;", iconFrame, StringComparison.Ordinal);
            Assert.DoesNotContain("_container.Stroke = new SolidColorBrush(BorderColor);", iconFrame, StringComparison.Ordinal);
        }

        [Fact]
        public void Material_progress_styles_use_quiet_tonal_role()
        {
            XDocument styles = LoadResourceDictionary(StylesResourcePath);

            IReadOnlyDictionary<string, string> implicitActivitySetters =
                GetImplicitStyleSetters(styles, "ActivityIndicator");
            IReadOnlyDictionary<string, string> thumbnailActivitySetters =
                GetStyleSetters(styles, "M3ThumbnailActivityIndicator");
            IReadOnlyDictionary<string, string> statusActivitySetters =
                GetStyleSetters(styles, "M3StatusActivityIndicator");
            IReadOnlyDictionary<string, string> implicitProgressSetters = GetImplicitStyleSetters(styles, "ProgressBar");
            IReadOnlyDictionary<string, string> linearProgressSetters = GetStyleSetters(styles, "M3LinearProgressBar");

            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}",
                implicitActivitySetters["Color"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}",
                thumbnailActivitySetters["Color"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}",
                statusActivitySetters["Color"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}",
                implicitProgressSetters["ProgressColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}",
                linearProgressSetters["ProgressColor"]);
            Assert.DoesNotContain("M3LightPrimary", implicitActivitySetters["Color"], StringComparison.Ordinal);
            Assert.DoesNotContain("M3DarkPrimary", statusActivitySetters["Color"], StringComparison.Ordinal);
            Assert.DoesNotContain("M3LightPrimary", implicitProgressSetters["ProgressColor"], StringComparison.Ordinal);
            Assert.DoesNotContain("M3DarkPrimary", linearProgressSetters["ProgressColor"], StringComparison.Ordinal);
        }

        [Fact]
        public void Light_theme_primary_fill_is_quieter_than_lime_accent()
        {
            string colors = LoadText(ColorsResourcePath);

            Assert.Contains("<Color x:Key=\"M3Accent\">#C6FF00</Color>", colors, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"M3DarkPrimary\">#C6FF00</Color>", colors, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"M3LightPrimary\">#4F6200</Color>", colors, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"M3LightPrimaryPressed\">#405100</Color>", colors, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"M3LightOnPrimary\">#FFFFFF</Color>", colors, StringComparison.Ordinal);
            Assert.DoesNotContain("<Color x:Key=\"M3Primary\">", colors, StringComparison.Ordinal);
            Assert.DoesNotContain("<Color x:Key=\"M3OnPrimary\">", colors, StringComparison.Ordinal);
            Assert.DoesNotContain("M3PrimaryBrush", colors, StringComparison.Ordinal);
            Assert.DoesNotContain("M3AccentPressed", colors, StringComparison.Ordinal);
            Assert.DoesNotContain("M3OnAccent", colors, StringComparison.Ordinal);
            Assert.DoesNotContain("M3AccentBrush", colors, StringComparison.Ordinal);
            Assert.DoesNotContain("<Color x:Key=\"M3LightPrimary\">#C6FF00</Color>", colors, StringComparison.Ordinal);
        }

        [Fact]
        public void Material_color_tokens_expose_full_light_and_dark_role_set()
        {
            string colors = LoadText(ColorsResourcePath);
            string[] roles =
            [
                "Primary",
                "OnPrimary",
                "PrimaryContainer",
                "OnPrimaryContainer",
                "Secondary",
                "OnSecondary",
                "SecondaryContainer",
                "OnSecondaryContainer",
                "Tertiary",
                "OnTertiary",
                "TertiaryContainer",
                "OnTertiaryContainer",
                "Error",
                "OnError",
                "ErrorContainer",
                "OnErrorContainer",
                "Surface",
                "SurfaceDim",
                "SurfaceBright",
                "OnSurface",
                "SurfaceVariant",
                "OnSurfaceVariant",
                "SurfaceContainerLowest",
                "SurfaceContainerLow",
                "SurfaceContainer",
                "SurfaceContainerHigh",
                "SurfaceContainerHighest",
                "Outline",
                "OutlineVariant",
                "InverseSurface",
                "InverseOnSurface",
                "InversePrimary",
                "Scrim",
            ];

            foreach (string role in roles)
            {
                Assert.Contains($"<Color x:Key=\"M3Light{role}\">", colors, StringComparison.Ordinal);
                Assert.Contains($"<Color x:Key=\"M3Dark{role}\">", colors, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Error_attention_surfaces_use_container_roles()
        {
            const string errorContainerBinding = "{AppThemeBinding Light={StaticResource M3LightErrorContainer}, Dark={StaticResource M3DarkErrorContainer}}";
            const string onErrorContainerBinding = "{AppThemeBinding Light={StaticResource M3LightOnErrorContainer}, Dark={StaticResource M3DarkOnErrorContainer}}";
            const string errorBinding = "{AppThemeBinding Light={StaticResource M3LightError}, Dark={StaticResource M3DarkError}}";

            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            XDocument type = LoadResourceDictionary(TypeResourcePath);
            IReadOnlyDictionary<string, string> attentionPanelSetters =
                GetStyleSetters(styles, "M3AttentionStatusPanel");
            IReadOnlyDictionary<string, string> attentionIconSetters =
                GetStyleSetters(styles, "M3AttentionStatusIcon");
            IReadOnlyDictionary<string, string> attentionIconFrameSetters =
                GetStyleSetters(styles, "M3AttentionNoticeIconFrame");
            IReadOnlyDictionary<string, string> attentionMessageSetters =
                GetStyleSetters(styles, "M3AttentionStatusMessage");
            IReadOnlyDictionary<string, string> fileErrorPanelSetters =
                GetStyleSetters(styles, "M3FileErrorStatusPanel");
            IReadOnlyDictionary<string, string> fileErrorIconSetters =
                GetStyleSetters(styles, "M3FileErrorStatusIcon");
            IReadOnlyDictionary<string, string> fileErrorTextSetters =
                GetStyleSetters(styles, "M3FileErrorStatusPrimaryText");
            IReadOnlyDictionary<string, string> fileAttentionChipSetters =
                GetStyleSetters(styles, "M3FileAttentionChip");
            IReadOnlyDictionary<string, string> errorChipLabelSetters =
                GetStyleSetters(type, "M3ErrorChipLabel");
            IReadOnlyDictionary<string, string> destructiveIconButtonSetters =
                GetStyleSetters(styles, "M3DestructiveFileChromeIconButton");
            IReadOnlyDictionary<string, string> destructiveActionSheetSetters =
                GetStyleSetters(styles, "M3ActionSheetDestructiveItem");

            Assert.Equal(errorContainerBinding, attentionPanelSetters["Stroke"]);
            Assert.Equal(errorContainerBinding, attentionPanelSetters["BackgroundColor"]);
            Assert.Equal(onErrorContainerBinding, attentionIconSetters["IconColor"]);
            Assert.Equal(errorContainerBinding, attentionIconFrameSetters["Stroke"]);
            Assert.Equal(errorContainerBinding, attentionIconFrameSetters["BackgroundColor"]);
            Assert.Equal(onErrorContainerBinding, attentionMessageSetters["TextColor"]);

            Assert.Equal(errorContainerBinding, fileErrorPanelSetters["Stroke"]);
            Assert.Equal(errorContainerBinding, fileErrorPanelSetters["BackgroundColor"]);
            Assert.Equal(onErrorContainerBinding, fileErrorIconSetters["IconColor"]);
            Assert.Equal(onErrorContainerBinding, fileErrorTextSetters["TextColor"]);
            Assert.Equal(errorContainerBinding, fileAttentionChipSetters["Stroke"]);
            Assert.Equal(errorContainerBinding, fileAttentionChipSetters["BackgroundColor"]);
            Assert.Equal(onErrorContainerBinding, errorChipLabelSetters["TextColor"]);

            Assert.Equal(errorBinding, destructiveIconButtonSetters["IconColor"]);
            Assert.Equal(errorBinding, destructiveActionSheetSetters["TextColor"]);
            Assert.Equal(errorBinding, destructiveActionSheetSetters["IconColor"]);
            Assert.Equal(errorBinding, destructiveActionSheetSetters["SelectedIconColor"]);
        }

        [Fact]
        public void App_background_surfaces_use_bright_and_dim_roles()
        {
            const string appSurfaceBinding = "{AppThemeBinding Light={StaticResource M3LightSurfaceBright}, Dark={StaticResource M3DarkSurfaceDim}}";
            const string darkSurfaceDim = "{StaticResource M3DarkSurfaceDim}";

            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> pageSetters = GetImplicitStyleSetters(styles, "Page");
            IReadOnlyDictionary<string, string> shellSetters = GetImplicitStyleSetters(styles, "Shell");
            IReadOnlyDictionary<string, string> documentViewerPageSetters =
                GetStyleSetters(styles, "M3DocumentViewerPage");
            IReadOnlyDictionary<string, string> documentViewerSurfaceSetters =
                GetStyleSetters(styles, "M3DocumentViewerSurface");
            IReadOnlyDictionary<string, string> documentViewerCollectionSetters =
                GetStyleSetters(styles, "M3DocumentViewerCollection");
            IReadOnlyDictionary<string, string> topAppBarSetters =
                GetStyleSetters(styles, "M3TopAppBarSurface");
            IReadOnlyDictionary<string, string> darkViewerPageSetters =
                GetStyleSetters(styles, "M3DarkViewerPage");
            IReadOnlyDictionary<string, string> darkViewerSurfaceSetters =
                GetStyleSetters(styles, "M3DarkViewerSurface");
            IReadOnlyDictionary<string, string> darkViewerCollectionSetters =
                GetStyleSetters(styles, "M3DarkViewerCollection");
            IReadOnlyDictionary<string, string> darkTopAppBarSetters =
                GetStyleSetters(styles, "M3DarkTopAppBarSurface");

            Assert.Equal(appSurfaceBinding, pageSetters["BackgroundColor"]);
            Assert.Equal(appSurfaceBinding, shellSetters["Shell.BackgroundColor"]);
            Assert.Equal(appSurfaceBinding, documentViewerPageSetters["BackgroundColor"]);
            Assert.Equal(appSurfaceBinding, documentViewerSurfaceSetters["BackgroundColor"]);
            Assert.Equal(appSurfaceBinding, documentViewerCollectionSetters["BackgroundColor"]);
            Assert.Equal(appSurfaceBinding, topAppBarSetters["BackgroundColor"]);
            Assert.Equal(darkSurfaceDim, darkViewerPageSetters["BackgroundColor"]);
            Assert.Equal(darkSurfaceDim, darkViewerSurfaceSetters["BackgroundColor"]);
            Assert.Equal(darkSurfaceDim, darkViewerCollectionSetters["BackgroundColor"]);
            Assert.Equal(darkSurfaceDim, darkTopAppBarSetters["BackgroundColor"]);
        }

        [Fact]
        public void Control_color_fallbacks_use_theme_primary_roles_not_generic_aliases()
        {
            string filledButton = LoadText(Path.Combine(ControlsDirectoryPath, "FilledButton.cs"));
            string toggleSwitch = LoadText(Path.Combine(ControlsDirectoryPath, "ToggleSwitch.cs"));

            Assert.Contains("MaterialResources.GetThemeColor(\"M3LightPrimary\", \"M3DarkPrimary\")", filledButton, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.GetThemeColor(\"M3LightPrimaryPressed\", \"M3DarkPrimaryPressed\")", filledButton, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.GetThemeColor(\"M3LightOnPrimary\", \"M3DarkOnPrimary\")", filledButton, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.GetThemeColor(\"M3LightPrimary\", \"M3DarkPrimary\")", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.GetThemeColor(\"M3LightPrimaryPressed\", \"M3DarkPrimaryPressed\")", toggleSwitch, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.GetThemeColor(\"M3LightOnPrimary\", \"M3DarkOnPrimary\")", toggleSwitch, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3Accent\")", filledButton, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3AccentPressed\")", filledButton, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3OnAccent\")", filledButton, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3Accent\")", toggleSwitch, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3AccentPressed\")", toggleSwitch, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3OnAccent\")", toggleSwitch, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3Primary\")", filledButton, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3OnPrimary\")", filledButton, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3Primary\")", toggleSwitch, StringComparison.Ordinal);
            Assert.DoesNotContain("MaterialResources.Get<Color>(\"M3OnPrimary\")", toggleSwitch, StringComparison.Ordinal);
        }

        [Fact]
        public void Android_system_bars_apply_theme_appearance_after_r()
        {
            string mainActivity = LoadText(MainActivityPath);
            string lightColors = LoadText(AndroidLightColorsPath);
            string darkColors = LoadText(AndroidDarkColorsPath);
            string lightStyles = LoadText(AndroidLightStylesPath);
            string darkStyles = LoadText(AndroidDarkStylesPath);

            Assert.Contains("using AndroidX.Core.View;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Resource.Color.cotton_system_bar_background", mainActivity, StringComparison.Ordinal);
            Assert.Contains("private Android.Views.View? _statusBarScrim;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("WindowCompat.SetDecorFitsSystemWindows(Window, true);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Window.SetStatusBarColor(systemBarColor);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("ApplyStatusBarScrim(systemBarColor);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Window.SetNavigationBarColor(systemBarColor);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("FirstSystemBarReapplyDelayMilliseconds = 250", mainActivity, StringComparison.Ordinal);
            Assert.Contains("SecondSystemBarReapplyDelayMilliseconds = 1000", mainActivity, StringComparison.Ordinal);
            Assert.Contains("ThirdSystemBarReapplyDelayMilliseconds = 2500", mainActivity, StringComparison.Ordinal);
            Assert.Contains("FourthSystemBarReapplyDelayMilliseconds = 5000", mainActivity, StringComparison.Ordinal);
            Assert.Contains("decorView.PostDelayed(ApplySystemBars, FirstSystemBarReapplyDelayMilliseconds);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("decorView.PostDelayed(ApplySystemBars, SecondSystemBarReapplyDelayMilliseconds);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("decorView.PostDelayed(ApplySystemBars, ThirdSystemBarReapplyDelayMilliseconds);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("decorView.PostDelayed(ApplySystemBars, FourthSystemBarReapplyDelayMilliseconds);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("new FrameLayout.LayoutParams(", mainActivity, StringComparison.Ordinal);
            Assert.Contains("GravityFlags.Top", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Resources.GetIdentifier(\"status_bar_height\", \"dimen\", \"android\")", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Android.Views.View decorView = Window.DecorView;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("WindowInsetsControllerCompat? compatInsetsController = WindowCompat.GetInsetsController(Window, decorView);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("if (compatInsetsController is not null)", mainActivity, StringComparison.Ordinal);
            Assert.Contains("bool useLightStatusBars = !isNightMode;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("bool useLightNavigationBars = !isNightMode;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("compatInsetsController.AppearanceLightStatusBars = useLightStatusBars;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("compatInsetsController.AppearanceLightNavigationBars = useLightNavigationBars;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("Window.InsetsController?.SetSystemBarsAppearance(appearance, mask);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("decorView.WindowInsetsController?.SetSystemBarsAppearance(appearance, mask);", mainActivity, StringComparison.Ordinal);
            Assert.Contains("decorView.SystemUiFlags = flags;", mainActivity, StringComparison.Ordinal);
            Assert.Contains("<color name=\"cotton_system_bar_background\">#F7F8F7</color>", lightColors, StringComparison.Ordinal);
            Assert.Contains("<color name=\"cotton_system_bar_background\">#090B0A</color>", darkColors, StringComparison.Ordinal);
            Assert.Contains("<item name=\"android:statusBarColor\">@color/cotton_system_bar_background</item>", lightStyles, StringComparison.Ordinal);
            Assert.Contains("<item name=\"android:statusBarColor\">@color/cotton_system_bar_background</item>", darkStyles, StringComparison.Ordinal);
            Assert.Contains("<item name=\"android:navigationBarColor\">@color/cotton_system_bar_background</item>", lightStyles, StringComparison.Ordinal);
            Assert.Contains("<item name=\"android:navigationBarColor\">@color/cotton_system_bar_background</item>", darkStyles, StringComparison.Ordinal);
            Assert.DoesNotContain("cotton_status_bar", mainActivity + lightColors + darkColors + lightStyles + darkStyles, StringComparison.Ordinal);
            Assert.DoesNotContain("cotton_surface", mainActivity + lightColors + darkColors + lightStyles + darkStyles, StringComparison.Ordinal);
            Assert.Contains("<item name=\"android:windowLightStatusBar\">true</item>", lightStyles, StringComparison.Ordinal);
            Assert.Contains("<item name=\"android:windowLightStatusBar\">false</item>", darkStyles, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Window.InsetsController?.SetSystemBarsAppearance(appearance, mask);\n                return;",
                mainActivity,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Material_control_color_defaults_are_theme_aware()
        {
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> darkDefaultProperties = GetControlDarkDefaultProperties();

            Assert.True(
                darkDefaultProperties.Count == 0,
                string.Join(
                    Environment.NewLine,
                    darkDefaultProperties
                        .OrderBy(item => item.Key, StringComparer.Ordinal)
                        .Select(item => $"{item.Key}: {string.Join(", ", item.Value.OrderBy(value => value, StringComparer.Ordinal))}")));
        }

        [Fact]
        public void Filled_button_typography_lives_in_material_style()
        {
            string filledButton = LoadText(Path.Combine(ControlsDirectoryPath, "FilledButton.cs"));
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> filledButtonSetters =
                GetStyleSetters(styles, "M3FilledButton");

            Assert.Equal("Bold", filledButtonSetters["FontAttributes"]);
            Assert.Contains("_label.FontAttributes = FontAttributes", filledButton, StringComparison.Ordinal);
            Assert.DoesNotContain("FontAttributes.Bold", filledButton, StringComparison.Ordinal);
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
            string fileListSkeletonView = LoadText(FileListSkeletonViewPath);
            string mainPage = LoadText(MainPagePath);

            Assert.Contains("<controls:FileListSkeletonView", mainPage, StringComparison.Ordinal);
            Assert.Contains("DefaultStyleResourceKey = \"M3FileListSkeletonView\"", fileListSkeletonView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultStyleResourceKey)", fileListSkeletonView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileListSkeletonView}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonRowGrid", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonPrimaryLineBlock", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileSkeletonSecondaryLineBlock", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_list_screens_use_initial_loading_skeletons()
        {
            string metadataListSkeletonView = LoadText(MetadataListSkeletonViewPath);

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
                Assert.DoesNotContain("Style=\"{StaticResource M3MetadataListSkeletonView}\"", page, StringComparison.Ordinal);
            }

            string recentFilesPage = LoadText(RecentFilesPagePath);

            Assert.Contains("DefaultStyleResourceKey = \"M3MetadataListSkeletonView\"", metadataListSkeletonView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultStyleResourceKey)", metadataListSkeletonView, StringComparison.Ordinal);
            Assert.Contains("IsBodyLineVisible=\"False\"", recentFilesPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Folder_picker_screens_use_file_loading_skeletons()
        {
            string destinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string fileListSkeletonView = LoadText(FileListSkeletonViewPath);

            Assert.Contains("<controls:FileListSkeletonView", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("IsLoadingPlaceholderVisible", destinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("DefaultStyleResourceKey = \"M3FileListSkeletonView\"", fileListSkeletonView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileListSkeletonView}\"", destinationPickerPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Outlined_inputs_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string materialDialogPage = LoadText(MaterialDialogPagePath);
            string focusedInputChromeBehavior = LoadText(FocusedInputChromeBehaviorPath);
            string materialMotion = LoadText(Path.Combine(ControlsDirectoryPath, "MaterialMotion.cs"));
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> outlinedInputSetters =
                GetStyleSetters(styles, "M3OutlinedInputField");

            Assert.Contains("<controls:OutlinedInputField", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:OutlinedInputField", trashPage, StringComparison.Ordinal);
            Assert.Contains("OutlinedInputField", materialDialogPage, StringComparison.Ordinal);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceContainerLow}, Dark={StaticResource M3DarkSurfaceContainerLow}}",
                outlinedInputSetters["BackgroundColor"]);
            Assert.Contains("MaterialMotion.UpdateBackgroundColor(", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateColor(", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("ApplyCurrentState(true)", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("ApplyRestingState(animate)", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("FieldBackgroundAnimationName = \"M3InputFieldBackground\"", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("IconFrameBackgroundAnimationName = \"M3InputIconFrameBackground\"", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("IconColorAnimationName = \"M3InputIconColor\"", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("LightSurfaceContainerLowResourceKey = \"M3LightSurfaceContainerLow\"", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("DarkSurfaceContainerLowResourceKey = \"M3DarkSurfaceContainerLow\"", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("return LightSurfaceContainerHighResourceKey;", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("return LightSurfaceContainerLowResourceKey;", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("return DarkSurfaceContainerHighResourceKey;", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("return DarkSurfaceContainerLowResourceKey;", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("FocusMotionDurationResourceKey = \"M3MotionFocusDuration\"", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.Contains("public static void UpdateColor(", materialMotion, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionFocusDuration\">120</x:Int32>", LoadText(InteractionResourcePath), StringComparison.Ordinal);
            Assert.DoesNotContain("<Entry", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Entry", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("FocusedInputChromeBehavior", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("FocusedInputChromeBehavior", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Field.BackgroundColor =", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.DoesNotContain("LeadingIconFrame.BackgroundColor =", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.DoesNotContain("LeadingIcon.IconColor =", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.DoesNotContain("LightSurfaceContainerLowestResourceKey", focusedInputChromeBehavior, StringComparison.Ordinal);
            Assert.DoesNotContain("DarkSurfaceContainerResourceKey", focusedInputChromeBehavior, StringComparison.Ordinal);
        }

        [Fact]
        public void Modal_pages_use_dynamic_material_style_resources()
        {
            string actionSheetItemView = LoadText(Path.Combine(ControlsDirectoryPath, "ActionSheetItemView.cs"));
            string materialDialogPage = LoadText(MaterialDialogPagePath);
            string materialActionSheetPage = LoadText(MaterialActionSheetPagePath);
            string combinedModalPages = materialDialogPage + materialActionSheetPage;
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> dialogSurfaceSetters =
                GetStyleSetters(styles, "M3DialogSurface");
            IReadOnlyDictionary<string, string> actionSheetSurfaceSetters =
                GetStyleSetters(styles, "M3ActionSheetSurface");
            IReadOnlyDictionary<string, string> actionSheetItemSetters =
                GetStyleSetters(styles, "M3ActionSheetItem");

            Assert.DoesNotContain("Style = MaterialResources.Get<Style>", combinedModalPages, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, \"M3ModalPage\")", materialDialogPage, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, \"M3ModalPage\")", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new BoxView(), \"M3ModalScrim\")", materialDialogPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new BoxView(), \"M3ModalScrim\")", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new VerticalStackLayout(), \"M3DialogStack\")", materialDialogPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new Border(), \"M3DialogSurface\")", materialDialogPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new HorizontalStackLayout(), \"M3DialogButtonRow\")", materialDialogPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new TextAction", materialDialogPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new FilledButton", materialDialogPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new VerticalStackLayout(), \"M3ActionSheetStack\")", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new Border(), \"M3ActionSheetSurface\")", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new Border(), \"M3ActionSheetHandle\")", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new ActionSheetItemView", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("ApplyStyle(new BoxView(), \"M3ActionSheetDivider\")", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("M3ActionSheetDestructiveItem", materialActionSheetPage, StringComparison.Ordinal);
            Assert.Contains("SelectedIconOpacityAnimationName = \"M3ActionSheetSelectedIconOpacity\"", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("SelectedIconScaleAnimationName = \"M3ActionSheetSelectedIconScale\"", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("OnSelectedPropertyChanged", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("M3MotionSelectionHiddenScale", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionSelectionDuration\")", actionSheetItemView, StringComparison.Ordinal);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceContainerHigh}, Dark={StaticResource M3DarkSurfaceContainerHigh}}",
                dialogSurfaceSetters["BackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceContainerLow}, Dark={StaticResource M3DarkSurfaceContainerLow}}",
                actionSheetSurfaceSetters["BackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3Transparent}, Dark={StaticResource M3Transparent}}",
                actionSheetItemSetters["RowBackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceContainerHigh}, Dark={StaticResource M3DarkSurfaceContainerHigh}}",
                actionSheetItemSetters["PressedRowBackgroundColor"]);
            Assert.DoesNotContain("_selectedIcon.IsVisible = IsSelected", actionSheetItemView, StringComparison.Ordinal);
        }

        [Fact]
        public void Action_sheet_items_animate_row_and_icon_frame_chrome()
        {
            string actionSheetItemView = LoadText(Path.Combine(ControlsDirectoryPath, "ActionSheetItemView.cs"));

            Assert.Contains("RowOpacityAnimationName = \"M3ActionSheetItemOpacity\"", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("IconFrameBackgroundAnimationName = \"M3ActionSheetIconFrameBackground\"", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("IconFrameBorderColorAnimationName = \"M3ActionSheetIconFrameBorderColor\"", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateBackgroundColor(", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateColor(", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("bool shouldAnimateChrome = animateChrome && _hasAppliedChromeState", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("ResolveCurrentIconFrameBorderColor()", actionSheetItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", actionSheetItemView, StringComparison.Ordinal);
            Assert.DoesNotContain(
                $"{Environment.NewLine}            Opacity = ResolvePressableOpacity(1);",
                actionSheetItemView,
                StringComparison.Ordinal);
            Assert.DoesNotContain("_iconFrame.BackgroundColor = IconFrameBackgroundColor;", actionSheetItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("_iconFrame.Stroke = new SolidColorBrush(IconFrameBorderColor);", actionSheetItemView, StringComparison.Ordinal);
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
            string centeredGateView = LoadText(CenteredGateViewPath);
            string emptyStateView = LoadText(EmptyStateViewPath);
            string interaction = LoadText(InteractionResourcePath);
            string styles = LoadText(StylesResourcePath);
            XDocument stylesDocument = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> emptyIconFrameSetters =
                GetStyleSetters(stylesDocument, "M3EmptyStateIconFrame");
            IReadOnlyDictionary<string, string> emptyIconSetters =
                GetStyleSetters(stylesDocument, "M3EmptyStateIcon");

            Assert.Contains("ActionCommand=\"{Binding ShowFileAddActionsCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsBodyVisible=\"{Binding Display.IsFilesEmptyDetailsVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ActionText=\"Choose folder\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("ActionIconButtonStyleResourceKey=\"M3PrimaryFileChromeIconButton\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey=\"M3CenteredPdfEmptyStateCard\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("IconFrameStyleResourceKey=\"M3PdfEmptyStateIconFrame\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:CenteredGateView>", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("<controls:EmptyStateView IconData=\"{x:Static controls:IconPathData.Device}\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("IsBusy=\"{Binding IsBusy}\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("IsActionEnabled=\"{Binding CanUnlock}\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("IsFilledAction=\"True\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey=\"M3AppLockCard\"", appLockGatePage, StringComparison.Ordinal);
            Assert.Contains("public class CenteredGateView", centeredGateView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3AppLockGateGrid\"", centeredGateView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star })", centeredGateView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto })", centeredGateView, StringComparison.Ordinal);
            Assert.Contains("Grid.SetRow(BodyContent, 1)", centeredGateView, StringComparison.Ordinal);
            Assert.Contains("IsBusyProperty", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("IsFilledActionProperty", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("FilledActionButtonStyleResourceKeyProperty", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("new LoadingIndicatorView", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("BodyOpacityAnimationName = \"M3EmptyStateBodyOpacity\"", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("BusyIndicatorOpacityAnimationName = \"M3EmptyStateBusyIndicatorOpacity\"", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("ActionRowOpacityAnimationName = \"M3EmptyStateActionRowOpacity\"", emptyStateView, StringComparison.Ordinal);
            Assert.Contains(
                "ActionIconOnlyButtonOpacityAnimationName = \"M3EmptyStateActionIconOnlyOpacity\"",
                emptyStateView,
                StringComparison.Ordinal);
            Assert.Contains(
                "FilledActionButtonOpacityAnimationName = \"M3EmptyStateFilledActionOpacity\"",
                emptyStateView,
                StringComparison.Ordinal);
            Assert.Contains("OnBusyPropertyChanged", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("CompleteElementVisibility", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("CompleteBusyState", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_loadingIndicator.IsVisible = IsBusy", emptyStateView, StringComparison.Ordinal);
            Assert.DoesNotContain("_body.IsVisible = IsBodyVisible", emptyStateView, StringComparison.Ordinal);
            Assert.DoesNotContain("_actionRow.IsVisible = isActionTextVisible", emptyStateView, StringComparison.Ordinal);
            Assert.DoesNotContain("_actionIconOnlyButton.IsVisible = isIconOnlyActionVisible", emptyStateView, StringComparison.Ordinal);
            Assert.DoesNotContain("_filledActionButton.IsVisible = isFilledActionVisible", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("new FilledButton", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("private readonly TouchSurfaceView _actionTouchSurface;", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("_actionTouchSurface = new TouchSurfaceView();", emptyStateView, StringComparison.Ordinal);
            Assert.Contains("_actionTouchSurface.TapCommand = IsActionEnabled ? actionCommand : null;", emptyStateView, StringComparison.Ordinal);
            Assert.DoesNotContain("LongPressBehavior", emptyStateView, StringComparison.Ordinal);
            Assert.DoesNotContain("M3ListItemTouchSurface", emptyStateView, StringComparison.Ordinal);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiaryContainer}, Dark={StaticResource M3DarkTertiaryContainer}}",
                emptyIconFrameSetters["BackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiaryContainer}, Dark={StaticResource M3DarkTertiaryContainer}}",
                emptyIconFrameSetters["Stroke"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightOnTertiaryContainer}, Dark={StaticResource M3DarkOnTertiaryContainer}}",
                emptyIconSetters["IconColor"]);
            Assert.DoesNotContain("M3LightPrimaryContainer", emptyIconFrameSetters["BackgroundColor"], StringComparison.Ordinal);
            Assert.DoesNotContain("M3DarkPrimaryContainer", emptyIconFrameSetters["Stroke"], StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.Row=\"1\"", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"*,Auto,*\"", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AppLockGateGrid}\"", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:EmptyStateView Grid.Row=\"1\"", appLockGatePage, StringComparison.Ordinal);
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
        public void Auth_shell_spacing_keeps_signed_out_screen_compact()
        {
            string spacing = LoadText(SpacingResourcePath);
            string styles = LoadText(StylesResourcePath);

            Assert.Contains("<Thickness x:Key=\"M3AuthShellMargin\">0,32,0,0</Thickness>", spacing, StringComparison.Ordinal);
            Assert.Contains("<Setter Property=\"Margin\" Value=\"{StaticResource M3AuthShellMargin}\" />", styles, StringComparison.Ordinal);
            Assert.DoesNotContain("<Thickness x:Key=\"M3AuthShellMargin\">0,56,0,0</Thickness>", spacing, StringComparison.Ordinal);
            Assert.DoesNotContain("<Thickness x:Key=\"M3AuthShellMargin\">0,88,0,0</Thickness>", spacing, StringComparison.Ordinal);
        }

        [Fact]
        public void Auth_surface_uses_compact_title_and_roomy_panel_tokens()
        {
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            string spacing = LoadText(SpacingResourcePath);

            XElement authTitleStyle = GetStyleByKey(styles, "M3AuthTitle");
            IReadOnlyDictionary<string, string> authTitleSetters = GetStyleSetters(styles, "M3AuthTitle");
            IReadOnlyDictionary<string, string> authPanelSetters = GetStyleSetters(styles, "M3AuthPanel");

            Assert.Equal("{StaticResource M3TitleLarge}", (string?)authTitleStyle.Attribute("BasedOn"));
            Assert.Equal("Bold", authTitleSetters["FontAttributes"]);
            Assert.Equal("1", authTitleSetters["MaxLines"]);
            Assert.Equal("TailTruncation", authTitleSetters["LineBreakMode"]);
            Assert.Equal("Center", authTitleSetters["VerticalOptions"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceContainerLow}, Dark={StaticResource M3DarkSurfaceContainerLow}}",
                authPanelSetters["BackgroundColor"]);
            Assert.Contains("<Thickness x:Key=\"M3AuthPanelPadding\">20</Thickness>", spacing, StringComparison.Ordinal);
            Assert.DoesNotContain("<Thickness x:Key=\"M3AuthPanelPadding\">16</Thickness>", spacing, StringComparison.Ordinal);
        }

        [Fact]
        public void Auth_sign_in_panel_uses_reusable_material_control()
        {
            string authSignInPanelView = LoadText(AuthSignInPanelViewPath);
            string mainPage = LoadText(MainPagePath);

            Assert.Contains("<controls:AuthSignInPanelView IsVisible=\"{Binding Display.IsSignInVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("InstanceUrl=\"{Binding Display.InstanceUrl, Mode=TwoWay}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Status=\"{Binding Display.Status}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisible=\"{Binding Display.IsStatusVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsInputEnabled=\"{Binding Display.IsInputEnabled}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ConnectCommand=\"{Binding ConnectCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class AuthSignInPanelView", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("DefaultCardStyleResourceKey = \"M3AuthPanel\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("DefaultFormStackStyleResourceKey = \"M3AuthFormStack\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("DefaultStatusTextStyleResourceKey = \"M3AuthStatus\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("DefaultButtonStyleResourceKey = \"M3AuthFilledButton\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("new OutlinedInputField", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("Placeholder = \"https://app.cottoncloud.dev/\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("SemanticHint = \"Cotton Cloud address\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("new ScreenStatusView", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("_status.IsStatusVisible = IsStatusVisible", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("new FilledButton", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("Text = \"Connect\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey = DefaultCardStyleResourceKey", authSignInPanelView, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ContentCardView IsVisible=\"{Binding Display.IsSignInVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3AuthFormStack}\">", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Placeholder=\"https://app.cottoncloud.dev/\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Text=\"Connect\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TextStyleResourceKey=\"M3AuthStatus\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthFilledButton}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Auth_legal_footer_uses_reusable_material_control()
        {
            string authLegalFooterView = LoadText(AuthLegalFooterViewPath);
            string mainPage = LoadText(MainPagePath);

            Assert.Contains("<controls:AuthLegalFooterView IsVisible=\"{Binding Display.IsLegalFooterVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("PrivacyCommand=\"{Binding PrivacyPolicyCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class AuthLegalFooterView", authLegalFooterView, StringComparison.Ordinal);
            Assert.Contains("DefaultFooterStyleResourceKey = \"M3LegalFooterBar\"", authLegalFooterView, StringComparison.Ordinal);
            Assert.Contains("DefaultPrivacyText = \"Privacy\"", authLegalFooterView, StringComparison.Ordinal);
            Assert.Contains("new HorizontalStackLayout", authLegalFooterView, StringComparison.Ordinal);
            Assert.Contains("new TextAction", authLegalFooterView, StringComparison.Ordinal);
            Assert.Contains("_footer.SetDynamicResource(StyleProperty, footerStyleResourceKey)", authLegalFooterView, StringComparison.Ordinal);
            Assert.Contains("_privacyAction.Command = PrivacyCommand", authLegalFooterView, StringComparison.Ordinal);
            Assert.DoesNotContain("<HorizontalStackLayout IsVisible=\"{Binding Display.IsLegalFooterVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:TextAction Text=\"Privacy\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3LegalFooterBar}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Loading_indicator_frames_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string appLockGatePage = LoadText(AppLockGatePagePath);
            string loadingStatusView = LoadText(LoadingStatusViewPath);
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains(
                "DetailMessageOpacityAnimationName = \"M3LoadingStatusDetailMessageOpacity\"",
                loadingStatusView,
                StringComparison.Ordinal);
            Assert.Contains(
                "ActionButtonOpacityAnimationName = \"M3LoadingStatusActionButtonOpacity\"",
                loadingStatusView,
                StringComparison.Ordinal);
            Assert.Contains("OnDetailMessageVisibilityPropertyChanged", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains("OnActionButtonVisibilityPropertyChanged", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains(
                "MaterialResources.Get<int>(\"M3MotionStatusDuration\")",
                loadingStatusView,
                StringComparison.Ordinal);
            Assert.Contains("CompleteDetailMessageVisibility", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains("CompleteActionButtonVisibility", loadingStatusView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border IsVisible=\"{Binding Display.IsLoadingVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border IsVisible=\"{Binding Display.IsAuthorizationProgressVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingStatusPanel", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingIndicatorFrame", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingActivityIndicator", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingIndicatorFrame", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3LoadingActivityIndicator", appLockGatePage, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_detailMessage.IsVisible = !string.IsNullOrWhiteSpace(detailText)",
                loadingStatusView,
                StringComparison.Ordinal);
            Assert.DoesNotContain("_actionButton.IsVisible = isActionVisible", loadingStatusView, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_screen_status_text_uses_reusable_material_control()
        {
            string authSignInPanelView = LoadText(AuthSignInPanelViewPath);
            string mainPage = LoadText(MainPagePath);
            string screenStatusView = LoadText(ScreenStatusViewPath);

            Assert.Contains("new ScreenStatusView", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("TextStyleResourceKey = DefaultStatusTextStyleResourceKey", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenStatusView Text=\"{Binding Display.ProfileStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisible=\"{Binding Display.IsProfileStatusVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("TextStyleResourceKey=\"M3BodyMedium\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisibleProperty", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("StatusOpacityAnimationName = \"M3ScreenStatusOpacity\"", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("OnStatusVisiblePropertyChanged", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("CompleteStatusVisibility", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("TextStyleResourceKeyProperty", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("DefaultTextStyleResourceKey = \"M3ScreenStatus\"", screenStatusView, StringComparison.Ordinal);
            Assert.Contains("_label.SetDynamicResource(StyleProperty, textStyleResourceKey)", screenStatusView, StringComparison.Ordinal);
            Assert.DoesNotContain("IsVisible=\"{Binding Display.IsProfileStatusVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Display.Status}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Display.ProfileStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthStatus}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3BodyMedium}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Retry_attention_panels_use_reusable_material_control()
        {
            string attentionStatusView = LoadText(AttentionStatusViewPath);
            string interaction = LoadText(InteractionResourcePath);
            string mainPage = LoadText(MainPagePath);
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);

            Assert.Contains("private readonly TouchSurfaceView _touchSurface;", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface = new TouchSurfaceView();", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.TapCommand = IsRowTapEnabled && IsActionEnabled ? actionCommand : null;", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains("ActionButtonOpacityAnimationName = \"M3AttentionStatusActionButtonOpacity\"", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains("OnActionButtonVisibilityPropertyChanged", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains(
                "nameof(IsActionVisible),\n            typeof(bool),\n            typeof(AttentionStatusView),\n            true,\n            propertyChanged: OnActionButtonVisibilityPropertyChanged);",
                attentionStatusView,
                StringComparison.Ordinal);
            Assert.Contains(
                "nameof(IsActionEnabled),\n            typeof(bool),\n            typeof(AttentionStatusView),\n            true,\n            propertyChanged: OnVisualPropertyChanged);",
                attentionStatusView,
                StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains("CompleteActionButtonVisibility", attentionStatusView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("LongPressBehavior", attentionStatusView, StringComparison.Ordinal);
            Assert.DoesNotContain("M3ListItemTouchSurface", attentionStatusView, StringComparison.Ordinal);
            Assert.DoesNotContain("_actionButton.IsVisible = IsActionVisible", attentionStatusView, StringComparison.Ordinal);

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
            string interaction = LoadText(InteractionResourcePath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> noticeIconFrameSetters =
                GetStyleSetters(styles, "M3FileNoticeIconFrame");
            IReadOnlyDictionary<string, string> noticeIconSetters =
                GetStyleSetters(styles, "M3FileNoticeIcon");

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
            Assert.Contains("TitleOpacityAnimationName = \"M3NoticePanelTitleOpacity\"", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("MessageOpacityAnimationName = \"M3NoticePanelMessageOpacity\"", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("ActionItemOpacityAnimationName = \"M3NoticePanelActionItemOpacity\"", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("OnTitleVisibilityPropertyChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("OnMessageVisibilityPropertyChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("OnActionTextVisibilityPropertyChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("OnActionCommandVisibilityPropertyChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("OnActionVisibilityPropertyChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("HasTextVisibilityChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("HasActionTextVisibilityChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("HasActionCommandVisibilityChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("HasActionVisibleFlagChanged", noticePanelView, StringComparison.Ordinal);
            Assert.Contains(
                "bool shouldAnimateTitleVisibility = animateTitleVisibility && _hasAppliedVisibilityState;",
                noticePanelView,
                StringComparison.Ordinal);
            Assert.Contains(
                "bool shouldAnimateMessageVisibility = animateMessageVisibility && _hasAppliedVisibilityState;",
                noticePanelView,
                StringComparison.Ordinal);
            Assert.Contains(
                "bool shouldAnimateActionVisibility = animateActionVisibility && _hasAppliedVisibilityState;",
                noticePanelView,
                StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("CompleteElementVisibility", noticePanelView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiaryContainer}, Dark={StaticResource M3DarkTertiaryContainer}}",
                noticeIconFrameSetters["BackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiaryContainer}, Dark={StaticResource M3DarkTertiaryContainer}}",
                noticeIconFrameSetters["Stroke"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightOnTertiaryContainer}, Dark={StaticResource M3DarkOnTertiaryContainer}}",
                noticeIconSetters["IconColor"]);
            Assert.DoesNotContain("M3LightPrimaryContainer", noticeIconFrameSetters["BackgroundColor"], StringComparison.Ordinal);
            Assert.DoesNotContain("M3DarkPrimaryContainer", noticeIconSetters["IconColor"], StringComparison.Ordinal);
            Assert.DoesNotContain("<Border IsVisible=\"{Binding Display.IsFilesNoticeVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticePanel", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticeGrid", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticeIconFrame", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("M3FileNoticeTextStack", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid IsVisible=\"{Binding IsRemotePushUnavailable}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionListItemView Grid.Row=\"2\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("_title.IsVisible = !string.IsNullOrWhiteSpace(title)", noticePanelView, StringComparison.Ordinal);
            Assert.DoesNotContain("_message.IsVisible = !string.IsNullOrWhiteSpace(message)", noticePanelView, StringComparison.Ordinal);
            Assert.DoesNotContain("_actionItem.IsVisible = isActionVisible", noticePanelView, StringComparison.Ordinal);
            Assert.DoesNotContain("bool shouldAnimateVisibility = _hasAppliedVisibilityState;", noticePanelView, StringComparison.Ordinal);
        }

        [Fact]
        public void Action_rows_use_reusable_material_control()
        {
            string actionListItemView = LoadText(ActionListItemViewPath);
            string activityFeedPage = LoadText(ActivityFeedPagePath);
            string backupSetupPage = LoadText(BackupSetupPagePath);
            string captureDestinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string recentFilesPage = LoadText(RecentFilesPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string storagePage = LoadText(StoragePagePath);
            string interaction = LoadText(InteractionResourcePath);

            Assert.Contains("private readonly TouchSurfaceView _touchSurface;", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface = new TouchSurfaceView();", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.TapCommand = IsActionEnabled ? rowTapCommand : null;", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("LeadingIconOpacityAnimationName = \"M3ActionListLeadingIconOpacity\"", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("SupportingTextOpacityAnimationName = \"M3ActionListSupportingTextOpacity\"", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("TrailingChipOpacityAnimationName = \"M3ActionListTrailingChipOpacity\"", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("OnLeadingIconVisibilityPropertyChanged", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("OnSupportingTextVisibilityPropertyChanged", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("OnTrailingChipVisibilityPropertyChanged", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteLeadingIconVisibility", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteSupportingTextVisibility", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteTrailingChipVisibility", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("ResolveLeadingIconLayoutVisibility", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("ResolveTrailingChipLayoutVisibility", actionListItemView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_leadingIcon.IsVisible = isLeadingIconVisible", actionListItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("_supportingText.IsVisible = IsSupportingTextVisible", actionListItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("_trailingChip.IsVisible = isTrailingTextVisible", actionListItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("LongPressBehavior", actionListItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("M3ListItemTouchSurface", actionListItemView, StringComparison.Ordinal);

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
        public void Touch_surfaces_use_rounded_material_state_layer()
        {
            XDocument stylesDocument = LoadResourceDictionary(StylesResourcePath);
            string styles = LoadText(StylesResourcePath);
            string touchSurfaceView = LoadText(TouchSurfaceViewPath);
            IReadOnlyDictionary<string, string> touchSurfaceSetters = GetStyleSetters(
                stylesDocument,
                "M3ListItemTouchSurface");

            Assert.Contains("public class TouchSurfaceView : Border", touchSurfaceView, StringComparison.Ordinal);
            Assert.Contains("_longPressBehavior = new LongPressBehavior();", touchSurfaceView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, \"M3ListItemTouchSurface\")", touchSurfaceView, StringComparison.Ordinal);
            Assert.Contains("<Style TargetType=\"controls:TouchSurfaceView\" x:Key=\"M3ListItemTouchSurface\">", styles, StringComparison.Ordinal);
            Assert.Contains(
                "MaterialResources.GetThemeColor(\n                        \"M3LightPressedStateLayer\",\n                        \"M3DarkPressedStateLayer\")",
                LoadText(LongPressBehaviorPath),
                StringComparison.Ordinal);
            Assert.Equal("{StaticResource M3Transparent}", touchSurfaceSetters["BackgroundColor"]);
            Assert.Equal("{StaticResource M3Transparent}", touchSurfaceSetters["Stroke"]);
            Assert.Equal("{StaticResource M3StrokeNone}", touchSurfaceSetters["StrokeThickness"]);
            Assert.Equal("Fill", touchSurfaceSetters["HorizontalOptions"]);
            Assert.Equal("Fill", touchSurfaceSetters["VerticalOptions"]);
            Assert.Contains(
                "<RoundRectangle CornerRadius=\"{StaticResource M3DefaultBorderCornerRadius}\" />",
                styles,
                StringComparison.Ordinal);
            Assert.DoesNotContain("<Style TargetType=\"Grid\" x:Key=\"M3ListItemTouchSurface\">", styles, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_browser_header_actions_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileBrowserTopBarView = LoadText(FileBrowserTopBarViewPath);
            string actionClusterView = LoadText(Path.Combine(ControlsDirectoryPath, "ActionClusterView.cs"));
            string initialsButton = LoadText(Path.Combine(ControlsDirectoryPath, "InitialsButton.cs"));
            string interaction = LoadText(InteractionResourcePath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> actionsContainerSetters =
                GetStyleSetters(styles, "M3FileBrowserActionsContainer");
            IReadOnlyDictionary<string, string> actionClusterSetters =
                GetStyleSetters(styles, "M3FileBrowserActionCluster");
            IReadOnlyDictionary<string, string> accountButtonSetters =
                GetStyleSetters(styles, "M3AccountInitialsButton");

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
            Assert.Contains("DefaultActionsContainerStyleResourceKey = \"M3FileBrowserActionsContainer\"", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultActionClusterStyleResourceKey = \"M3FileBrowserActionCluster\"", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("new ActionClusterView", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("new InitialsButton", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("_upButtonHost = new ContentView", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("IsSearchActive ? IconPathData.Close : IconPathData.Search", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("_actionsContainer.SetDynamicResource(StyleProperty, actionsContainerStyleResourceKey)", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("_actionCluster.ClusterStyleResourceKey = actionClusterStyleResourceKey", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("NavigateUpButtonOpacityAnimationName = \"M3FileBrowserNavigateUpButtonOpacity\"", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("PathTextOpacityAnimationName = \"M3FileBrowserPathTextOpacity\"", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("StatusTextOpacityAnimationName = \"M3FileBrowserStatusTextOpacity\"", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("OnNavigateUpVisibilityPropertyChanged", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("OnPathTextVisibilityPropertyChanged", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("OnStatusTextVisibilityPropertyChanged", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionButtonOpacityAnimationName = \"M3ActionClusterPrimaryButtonOpacity\"", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionButtonOpacityAnimationName = \"M3ActionClusterSecondaryButtonOpacity\"", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionButtonOpacityAnimationName = \"M3ActionClusterTertiaryButtonOpacity\"", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("QuaternaryActionButtonOpacityAnimationName = \"M3ActionClusterQuaternaryButtonOpacity\"", actionClusterView, StringComparison.Ordinal);
            Assert.Equal(4, CountOccurrences(actionClusterView, "propertyChanged: OnActionVisibilityPropertyChanged);"));
            Assert.Contains("private static void OnActionVisibilityPropertyChanged", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("view.UpdateVisualState(animateActionVisibility: true);", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("view.UpdateVisualState(animateActionVisibility: false);", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("bool shouldAnimateVisibility = animateActionVisibility && _hasAppliedActionVisibilityState;", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("CompleteNavigateUpButtonVisibility", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("CompletePathTextVisibility", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("CompleteStatusTextVisibility", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.Contains("CompleteActionButtonVisibility", actionClusterView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.Equal("{StaticResource Space8}", actionsContainerSetters["Spacing"]);
            Assert.Equal("{StaticResource Space4}", actionClusterSetters["Spacing"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightPrimary}, Dark={StaticResource M3DarkPrimary}}",
                accountButtonSetters["TextColor"]);
            Assert.Equal("Bold", accountButtonSetters["TextFontAttributes"]);
            Assert.Contains("TextFontAttributesProperty", initialsButton, StringComparison.Ordinal);
            Assert.Contains("_label.FontAttributes = TextFontAttributes", initialsButton, StringComparison.Ordinal);
            Assert.DoesNotContain("FontAttributes = FontAttributes.Bold", initialsButton, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid ColumnDefinitions=\"Auto,*,Auto\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileBrowserTopBar}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionClusterView ClusterStyleResourceKey=\"M3FileBrowserActionCluster\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:InitialsButton Text=\"{Binding Display.ProfileInitials}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Search}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Sort}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.ViewTiles}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"controls:ActionClusterView\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Property=\"PrimaryActionIconData\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("_upButton.IsVisible = IsNavigateUpVisible", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.DoesNotContain("_pathText.IsVisible = IsPathTextVisible", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.DoesNotContain("_statusText.IsVisible = IsStatusTextVisible", fileBrowserTopBarView, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "actionButton.IsVisible = isVisible && iconData is not null && command is not null",
                actionClusterView,
                StringComparison.Ordinal);
            Assert.DoesNotContain("bool shouldAnimateVisibility = _hasAppliedActionVisibilityState;", actionClusterView, StringComparison.Ordinal);
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
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains("trailingContent = ResolveTrailingContent(trailingText)", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("QuaternaryDetailTextProperty", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("_trailingContentHost.Content = trailingContent", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains(
                "PrimaryDetailTextOpacityAnimationName = \"M3SettingsSectionPrimaryDetailOpacity\"",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecondaryDetailTextOpacityAnimationName = \"M3SettingsSectionSecondaryDetailOpacity\"",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "TertiaryDetailTextOpacityAnimationName = \"M3SettingsSectionTertiaryDetailOpacity\"",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "QuaternaryDetailTextOpacityAnimationName = \"M3SettingsSectionQuaternaryDetailOpacity\"",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "LeadingIconOpacityAnimationName = \"M3SettingsSectionLeadingIconOpacity\"",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "TrailingContentOpacityAnimationName = \"M3SettingsSectionTrailingContentOpacity\"",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnPrimaryDetailTextVisibilityPropertyChanged",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnSecondaryDetailTextVisibilityPropertyChanged",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnTertiaryDetailTextVisibilityPropertyChanged",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnQuaternaryDetailTextVisibilityPropertyChanged",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnLeadingIconVisibilityPropertyChanged",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnTrailingContentVisibilityPropertyChanged",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains(
                "MaterialResources.Get<int>(\"M3MotionStatusDuration\")",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains("CompletePrimaryDetailTextVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteSecondaryDetailTextVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteTertiaryDetailTextVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteQuaternaryDetailTextVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteLeadingIconVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteTrailingContentVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("ResolveLeadingIconLayoutVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("ResolveTrailingContentLayoutVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_leadingIcon.IsVisible = isLeadingIconVisible", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain("_trailingContentHost.IsVisible = isTrailingContentVisible", settingsSectionHeaderView, StringComparison.Ordinal);
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
            Assert.DoesNotContain(
                "_primaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(primaryDetailText)",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_secondaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(secondaryDetailText)",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_tertiaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(tertiaryDetailText)",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_quaternaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(quaternaryDetailText)",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Top_app_bar_actions_use_reusable_material_control()
        {
            string topAppBar = LoadText(TopAppBarPath);
            string topAppBarCodeBehind = LoadText(TopAppBarCodeBehindPath);
            string topAppBarContentGridView = LoadText(TopAppBarContentGridViewPath);
            string topAppBarTitleLabel = LoadText(TopAppBarTitleLabelPath);

            Assert.Equal(1, CountOccurrences(topAppBar, "<controls:IconButton"));
            Assert.Contains("<controls:TopAppBarContentGridView>", topAppBar, StringComparison.Ordinal);
            Assert.Contains("<controls:IconButton x:Name=\"BackButton\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("<controls:TopAppBarTitleLabel Grid.Column=\"1\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("UseDarkTheme=\"{Binding Source={x:Reference Root}, Path=UseDarkTheme}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("<controls:ActionClusterView x:Name=\"Actions\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionIconData=\"{Binding Source={x:Reference Root}, Path=PrimaryIconData}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("PrimaryActionCommand=\"{Binding Source={x:Reference Root}, Path=PrimaryCommand}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("IsPrimaryActionVisible=\"{Binding Source={x:Reference Root}, Path=IsPrimaryActionVisible}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("SecondaryActionCommand=\"{Binding Source={x:Reference Root}, Path=SecondaryCommand}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("IsSecondaryActionVisible=\"{Binding Source={x:Reference Root}, Path=IsSecondaryActionVisible}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("TertiaryActionCommand=\"{Binding Source={x:Reference Root}, Path=TertiaryCommand}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("IsTertiaryActionVisible=\"{Binding Source={x:Reference Root}, Path=IsTertiaryActionVisible}\"", topAppBar, StringComparison.Ordinal);
            Assert.Contains("DefaultSurfaceStyleResourceKey = \"M3TopAppBarSurface\"", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("DarkSurfaceStyleResourceKey = \"M3DarkTopAppBarSurface\"", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("DefaultActionClusterStyleResourceKey = \"M3TopAppBarActionCluster\"", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("DefaultActionIconButtonStyleResourceKey = \"M3TopAppBarIconButton\"", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("DarkActionIconButtonStyleResourceKey = \"M3DarkTopAppBarIconButton\"", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("propertyChanged: OnVisualPropertyChanged", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, surfaceStyleResourceKey)", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("BackButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey)", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("Actions.ClusterStyleResourceKey = DefaultActionClusterStyleResourceKey", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("Actions.PrimaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("Actions.SecondaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("Actions.TertiaryActionIconButtonStyleResourceKey = actionIconButtonStyleResourceKey", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.Contains("public class TopAppBarContentGridView : Grid", topAppBarContentGridView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3TopAppBarContentGrid\"", topAppBarContentGridView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<double>(\"TouchTarget\")", topAppBarContentGridView, StringComparison.Ordinal);
            Assert.Contains("ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(touchTarget) })", topAppBarContentGridView, StringComparison.Ordinal);
            Assert.Contains("ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star })", topAppBarContentGridView, StringComparison.Ordinal);
            Assert.Contains("ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto })", topAppBarContentGridView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultGridStyleResourceKey)", topAppBarContentGridView, StringComparison.Ordinal);
            Assert.Contains("public class TopAppBarTitleLabel : Label", topAppBarTitleLabel, StringComparison.Ordinal);
            Assert.Contains("DefaultTitleStyleResourceKey = \"M3AppBarTitleLine\"", topAppBarTitleLabel, StringComparison.Ordinal);
            Assert.Contains("DarkTitleStyleResourceKey = \"M3DarkAppBarTitleLine\"", topAppBarTitleLabel, StringComparison.Ordinal);
            Assert.Contains("UseDarkThemeProperty", topAppBarTitleLabel, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, titleStyleResourceKey)", topAppBarTitleLabel, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid.ColumnDefinitions>", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<ColumnDefinition", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<ContentView.Triggers>", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton.Triggers>", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label.Triggers>", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionClusterView.Triggers>", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("DataTrigger", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("<HorizontalStackLayout Grid.Column=\"2\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Source={x:Reference Root}, Path=PrimaryDescription}\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Source={x:Reference Root}, Path=SecondaryDescription}\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Source={x:Reference Root}, Path=TertiaryDescription}\"", topAppBar, StringComparison.Ordinal);
            Assert.DoesNotContain("ContentGrid.SetDynamicResource", topAppBarCodeBehind, StringComparison.Ordinal);
            Assert.DoesNotContain("TitleLabel.SetDynamicResource", topAppBarCodeBehind, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_page_root_uses_reusable_material_shell()
        {
            string mainPage = LoadText(MainPagePath);
            string mainPageRootView = LoadText(MainPageRootViewPath);

            Assert.Contains("<controls:MainPageRootView x:Name=\"RootLayout\">", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class MainPageRootView : Grid", mainPageRootView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3MainPageRootGrid\"", mainPageRootView, StringComparison.Ordinal);
            Assert.Contains("RowDefinitions.Add(new RowDefinition { Height = GridLength.Star })", mainPageRootView, StringComparison.Ordinal);
            Assert.Contains("RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto })", mainPageRootView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultGridStyleResourceKey)", mainPageRootView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid x:Name=\"RootLayout\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("RowDefinitions=\"*,Auto,Auto\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3MainPageRootGrid}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_browser_refresh_uses_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string materialRefreshView = LoadText(MaterialRefreshViewPath);
            string styles = LoadText(StylesResourcePath);

            Assert.Contains("<controls:MaterialRefreshView Grid.Row=\"0\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding RefreshFilesCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsRefreshing=\"{Binding Display.IsFilesRefreshing, Mode=TwoWay}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class MaterialRefreshView : RefreshView", materialRefreshView, StringComparison.Ordinal);
            Assert.Contains("[ContentProperty(nameof(Content))]", materialRefreshView, StringComparison.Ordinal);
            Assert.Contains("DefaultRefreshStyleResourceKey = \"M3MaterialRefreshView\"", materialRefreshView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultRefreshStyleResourceKey)", materialRefreshView, StringComparison.Ordinal);
            Assert.Contains("<Style TargetType=\"RefreshView\" x:Key=\"M3RefreshViewBase\">", styles, StringComparison.Ordinal);
            Assert.Contains("<Setter Property=\"RefreshColor\" Value=\"{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}\" />", styles, StringComparison.Ordinal);
            Assert.Contains("<Style TargetType=\"RefreshView\" BasedOn=\"{StaticResource M3RefreshViewBase}\" />", styles, StringComparison.Ordinal);
            Assert.Contains("<Style TargetType=\"controls:MaterialRefreshView\" x:Key=\"M3MaterialRefreshView\" BasedOn=\"{StaticResource M3RefreshViewBase}\" />", styles, StringComparison.Ordinal);
            Assert.DoesNotContain("<RefreshView Grid.Row=\"0\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void File_status_action_rows_use_reusable_material_control()
        {
            string fileStatusActionView = LoadText(FileStatusActionViewPath);
            string interaction = LoadText(InteractionResourcePath);
            string mainPage = LoadText(MainPagePath);

            Assert.Contains("private readonly TouchSurfaceView _touchSurface;", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface = new TouchSurfaceView();", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.TapCommand = IsActionEnabled ? command : null;", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("DetailsOpacityAnimationName = \"M3FileStatusDetailsOpacity\"", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("OnDetailsVisibilityPropertyChanged", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("CompleteDetailsVisibility", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("_detailsColumn.Width = new GridLength(0)", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumnSpan(_text, 2)", fileStatusActionView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("LongPressBehavior", fileStatusActionView, StringComparison.Ordinal);
            Assert.DoesNotContain("M3ListItemTouchSurface", fileStatusActionView, StringComparison.Ordinal);
            Assert.DoesNotContain("_details.IsVisible = !string.IsNullOrWhiteSpace", fileStatusActionView, StringComparison.Ordinal);

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

            Assert.Contains("<controls:StackedItemsView IsVisible=\"{Binding Display.IsFileListViewVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding Display.FileEntries}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("StackStyleResourceKey=\"M3FileListStack\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:StackedItemsView.ItemTemplate>", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:FileListEntryRowView Title=\"{Binding Name}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("Detail=\"{Binding DisplayDetails}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class FileListEntryRowView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("_grid.SetDynamicResource(StyleProperty, \"M3FileListRowGrid\")", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<double>(\"M3FileListThumbnailColumnWidth\")", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<double>(\"M3FileActionSize\")", fileListEntryRowView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Style=\"{StaticResource M3FileListRowGrid}\">", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ColumnDefinition Width=\"{StaticResource M3FileListThumbnailColumnWidth}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ColumnDefinition Width=\"{StaticResource M3FileActionSize}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3FileListStack}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("BindableLayout.ItemsSource=\"{Binding Display.FileEntries}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Selection_bars_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string selectionBarView = LoadText(SelectionBarViewPath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);

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

            Assert.Contains("private readonly ActionClusterView _actions;", selectionBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultActionClusterStyleResourceKey = \"M3SelectionBarActionCluster\"", selectionBarView, StringComparison.Ordinal);
            Assert.Contains("_actions.PrimaryActionCommand = PrimaryActionCommand", selectionBarView, StringComparison.Ordinal);
            Assert.Contains("_actions.SecondaryActionCommand = SecondaryActionCommand", selectionBarView, StringComparison.Ordinal);
            Assert.Contains("_actions.TertiaryActionCommand = TertiaryActionCommand", selectionBarView, StringComparison.Ordinal);
            Assert.Contains("_actions.IsQuaternaryActionVisible = false", selectionBarView, StringComparison.Ordinal);
            Assert.DoesNotContain("private readonly IconButton _primaryActionButton;", selectionBarView, StringComparison.Ordinal);
            Assert.DoesNotContain("private static void UpdateActionButton(", selectionBarView, StringComparison.Ordinal);
            Assert.Contains(
                "x:Key=\"M3SelectionBarActionCluster\" BasedOn=\"{StaticResource M3RowActionCluster}\"",
                styles.ToString(SaveOptions.DisableFormatting),
                StringComparison.Ordinal);
        }

        [Fact]
        public void Selection_overlays_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string selectionOverlayView = LoadText(SelectionOverlayViewPath);
            string interaction = LoadText(InteractionResourcePath);

            Assert.Equal(0, CountOccurrences(mainPage, "<controls:SelectionOverlayView"));
            Assert.Contains("new SelectionOverlayView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("new SelectionOverlayView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("Grid.SetColumnSpan(_selectionOverlay, 3)", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("Grid.SetRowSpan(_selectionOverlay, 2)", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("OverlayStyleResourceKey = \"M3FileSelectionRowOverlay\"", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("DefaultOverlayStyleResourceKey = \"M3FileSelectionOverlay\"", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("InputTransparent = true", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("SelectionOverlayOpacityAnimationName = \"M3FileSelectionOverlayOpacity\"", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("OnSelectedPropertyChanged", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionSelectionDuration\")", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("M3MotionVisibleOpacity", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("M3MotionHiddenOpacity", selectionOverlayView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionSelectionDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_overlay.IsVisible = IsSelected", selectionOverlayView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.ColumnSpan=\"3\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.RowSpan=\"2\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileSelectionRowOverlay}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileSelectionOverlay}\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Bottom_navigation_uses_reusable_material_shell()
        {
            string fileBrowserNavigationBarView = LoadText(FileBrowserNavigationBarViewPath);
            string mainPage = LoadText(MainPagePath);
            string navigationBarItem = LoadText(Path.Combine(ControlsDirectoryPath, "NavigationBarItem.cs"));
            string navigationBarView = LoadText(NavigationBarViewPath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            IReadOnlyDictionary<string, string> navigationItemSetters =
                GetStyleSetters(styles, "M3NavigationBarItem");

            Assert.Contains("<controls:FileBrowserNavigationBarView Grid.Row=\"2\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding Display.IsFileBrowserQuickNavigationVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("FilesText=\"{Binding Display.FilesNavigation.Label}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("FilesSemanticDescription=\"{Binding Display.FilesNavigation.AccessibilityText}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("SyncCommand=\"{Binding OpenSyncSettingsCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsSyncEnabled=\"{Binding Display.IsProfileVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("BackupText=\"{Binding Display.BackupNavigation.Label}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("BackupCommand=\"{Binding OpenBackupSetupCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("MoreCommand=\"{Binding AccountCommand}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("IsMoreEnabled=\"{Binding Display.IsAccountActionEnabled}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("public class FileBrowserNavigationBarView", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("new NavigationBarView", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Equal(4, CountOccurrences(fileBrowserNavigationBarView, "CreateItem(IconPathData."));
            Assert.Contains("CreateItem(IconPathData.Folder, DefaultSelectedItemStyleResourceKey, 0)", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("CreateItem(IconPathData.Transfer, DefaultUnselectedItemStyleResourceKey, 1)", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("CreateItem(IconPathData.Backup, DefaultUnselectedItemStyleResourceKey, 2)", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("CreateItem(IconPathData.MoreVertical, DefaultUnselectedItemStyleResourceKey, 3)", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultSelectedItemStyleResourceKey = \"M3NavigationBarItemSelected\"", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultUnselectedItemStyleResourceKey = \"M3NavigationBarItemUnselected\"", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("SemanticProperties.SetDescription(item, semanticDescription ?? string.Empty)", fileBrowserNavigationBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultColumnCount = 4", navigationBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3NavigationBarGrid\"", navigationBarView, StringComparison.Ordinal);
            Assert.Contains("DefaultSurfaceStyleResourceKey = \"M3NavigationBarSurface\"", navigationBarView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _grid.Children", navigationBarView, StringComparison.Ordinal);
            Assert.Equal("Bold", navigationItemSetters["TextFontAttributes"]);
            Assert.Contains("TextFontAttributesProperty", navigationBarItem, StringComparison.Ordinal);
            Assert.Contains("_label.FontAttributes = TextFontAttributes", navigationBarItem, StringComparison.Ordinal);
            Assert.DoesNotContain("FontAttributes = FontAttributes.Bold", navigationBarItem, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:NavigationBarView Grid.Row=\"2\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:NavigationBarItem", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Border Grid.Row=\"2\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NavigationBarSurface}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NavigationBarGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NavigationBarItemSelected}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3NavigationBarItemUnselected}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ColumnDefinitions=\"*,*,*,*\"", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void File_entry_thumbnails_use_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string trashPage = LoadText(TrashPagePath);
            string fileListEntryRowView = LoadText(FileListEntryRowViewPath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string fileThumbnailView = LoadText(Path.Combine(ControlsDirectoryPath, "FileThumbnailView.cs"));
            string trashListEntryCardView = LoadText(TrashListEntryCardViewPath);
            string trashTileEntryCardView = LoadText(TrashTileEntryCardViewPath);
            string interaction = LoadText(InteractionResourcePath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            XDocument type = LoadResourceDictionary(TypeResourcePath);
            IReadOnlyDictionary<string, string> folderThumbnailIconSetters =
                GetStyleSetters(styles, "M3FolderThumbnailIcon");
            IReadOnlyDictionary<string, string> folderThumbnailFrameSetters =
                GetStyleSetters(styles, "M3FolderThumbnailFrame");
            IReadOnlyDictionary<string, string> fileThumbnailSurfaceSetters =
                GetStyleSetters(styles, "M3FileThumbnailSurface");
            IReadOnlyDictionary<string, string> filePreviewSurfaceSetters =
                GetStyleSetters(styles, "M3FilePreviewSurface");
            IReadOnlyDictionary<string, string> thumbnailIconFrameSetters =
                GetStyleSetters(styles, "M3ThumbnailIconFrame");
            IReadOnlyDictionary<string, string> thumbnailPlaceholderSetters =
                GetStyleSetters(type, "M3ThumbnailPlaceholder");

            Assert.Equal(0, CountOccurrences(mainPage, "<controls:FileThumbnailView"));
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceVariant}, Dark={StaticResource M3DarkSurfaceVariant}}",
                fileThumbnailSurfaceSetters["BackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceVariant}, Dark={StaticResource M3DarkSurfaceVariant}}",
                filePreviewSurfaceSetters["BackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceVariant}, Dark={StaticResource M3DarkSurfaceVariant}}",
                thumbnailIconFrameSetters["FrameBackgroundColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}",
                folderThumbnailIconSetters["IconColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightTertiary}, Dark={StaticResource M3DarkTertiary}}",
                folderThumbnailFrameSetters["IconColor"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightOnSurfaceVariant}, Dark={StaticResource M3DarkOnSurfaceVariant}}",
                thumbnailPlaceholderSetters["TextColor"]);
            Assert.Contains("SurfaceStyleResourceKey = \"M3FilePreviewSurface\"", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("SelectionMarkStyleResourceKey = \"M3FileTileSelectionMark\"", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("FolderIconSize=\"{Binding Source={x:Reference RootPage}, Path=FileTileFolderIconSize}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("new FileThumbnailView", fileListEntryRowView, StringComparison.Ordinal);
            Assert.Contains("new FileThumbnailView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("PreviewImageOpacityAnimationName = \"M3FileThumbnailPreviewOpacity\"", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("FolderIconOpacityAnimationName = \"M3FileThumbnailFolderOpacity\"", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("PlaceholderOpacityAnimationName = \"M3FileThumbnailPlaceholderOpacity\"", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("LoadingIndicatorOpacityAnimationName = \"M3FileThumbnailLoadingOpacity\"", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("BadgeOpacityAnimationName = \"M3FileThumbnailBadgeOpacity\"", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("SelectionMarkOpacityAnimationName = \"M3FileSelectionMarkOpacity\"", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("SelectionMarkScaleAnimationName = \"M3FileSelectionMarkScale\"", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("OnPreviewImageVisibilityPropertyChanged", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("OnFolderIconVisibilityPropertyChanged", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("OnPlaceholderVisibilityPropertyChanged", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("OnLoadingPropertyChanged", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("OnBadgeVisibilityPropertyChanged", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("OnSelectedPropertyChanged", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("CompletePreviewImageVisibility", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("CompleteFolderIconVisibility", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("CompletePlaceholderVisibility", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("CompleteBadgeVisibility", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("IsPlaceholderActuallyVisible", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("IsBadgeActuallyVisible", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("!string.IsNullOrWhiteSpace(BadgeText)", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("M3MotionSelectionHiddenScale", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionSelectionDuration\")", fileThumbnailView, StringComparison.Ordinal);
            Assert.Contains("<x:Double x:Key=\"M3MotionSelectionHiddenScale\">0.82</x:Double>", interaction, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionSelectionDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_image.IsVisible = IsPreviewImageVisible", fileThumbnailView, StringComparison.Ordinal);
            Assert.DoesNotContain("_folderIcon.IsVisible = IsFolderThumbnailVisible", fileThumbnailView, StringComparison.Ordinal);
            Assert.DoesNotContain("_placeholder.IsVisible = IsPlaceholderTextVisible", fileThumbnailView, StringComparison.Ordinal);
            Assert.DoesNotContain("_loadingIndicator.IsVisible = IsLoading", fileThumbnailView, StringComparison.Ordinal);
            Assert.DoesNotContain("_badge.IsVisible = IsBadgeVisible", fileThumbnailView, StringComparison.Ordinal);
            Assert.DoesNotContain("_selectionMark.IsVisible = IsSelected", fileThumbnailView, StringComparison.Ordinal);

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
        public void List_touch_state_layer_uses_material_motion()
        {
            string longPressBehavior = LoadText(LongPressBehaviorPath);
            string materialMotion = LoadText(Path.Combine(ControlsDirectoryPath, "MaterialMotion.cs"));
            string materialResources = LoadText(Path.Combine(ControlsDirectoryPath, "MaterialResources.cs"));

            Assert.Contains("StateLayerAnimationName = \"M3ListItemStateLayer\"", longPressBehavior, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateBackgroundColor(", longPressBehavior, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionPressInDuration\")", longPressBehavior, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionPressOutDuration\")", longPressBehavior, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.GetThemeColor(", longPressBehavior, StringComparison.Ordinal);
            Assert.Contains("public static void UpdateBackgroundColor(", materialMotion, StringComparison.Ordinal);
            Assert.Contains("public static void AnimateBackgroundColor(", materialMotion, StringComparison.Ordinal);
            Assert.Contains("Animation animation = new(", materialMotion, StringComparison.Ordinal);
            Assert.Contains("element.Handler is not null", materialMotion, StringComparison.Ordinal);
            Assert.Contains("Easing.CubicOut", materialMotion, StringComparison.Ordinal);
            Assert.Contains("public static Color GetThemeColor(", materialResources, StringComparison.Ordinal);
            Assert.DoesNotContain("VisualElement.BackgroundColorProperty", longPressBehavior, StringComparison.Ordinal);
        }

        [Fact]
        public void Material_style_key_resolution_is_centralized()
        {
            string materialResources = LoadText(Path.Combine(ControlsDirectoryPath, "MaterialResources.cs"));
            string repositoryRoot = FindRepositoryRoot(StylesResourcePath);
            string controlsPath = Path.Combine(repositoryRoot, ControlsDirectoryPath);

            Assert.Contains("public static string ResolveStyleResourceKey(", materialResources, StringComparison.Ordinal);
            Assert.Contains("string.IsNullOrWhiteSpace(resourceKey)", materialResources, StringComparison.Ordinal);

            string[] baseContainerControls =
            [
                "ContentCardView.cs",
                "LayeredContentView.cs",
                "MaterialCollectionView.cs",
                "ScreenContentGridView.cs",
                "ScreenScrollBodyView.cs",
                "ScreenShellView.cs",
                "SettingsCardView.cs",
                "StackedContentView.cs",
                "StackedItemsView.cs",
                "WrappedItemsView.cs",
            ];
            (string ControlName, int ExpectedResolutionCount)[] statusFeedbackControls =
            [
                ("AttentionStatusView.cs", 4),
                ("EmptyStateView.cs", 5),
                ("LinearProgressView.cs", 1),
                ("LoadingStatusView.cs", 6),
                ("ScreenStatusView.cs", 1),
                ("ViewerStatusOverlayView.cs", 1),
            ];
            (string ControlName, int ExpectedResolutionCount)[] fileItemControls =
            [
                ("ChipView.cs", 2),
                ("FileEntryActionButtonView.cs", 1),
                ("FileEntryTextView.cs", 3),
                ("FileListMetadataView.cs", 5),
                ("FileThumbnailView.cs", 4),
                ("FileTileMetadataView.cs", 8),
                ("SelectionOverlayView.cs", 1),
            ];
            (string ControlName, int ExpectedResolutionCount)[] actionMetadataControls =
            [
                ("ActionListItemView.cs", 8),
                ("MetadataCardBodyView.cs", 7),
                ("MetadataCardHeaderView.cs", 7),
                ("MetadataCardView.cs", 3),
            ];
            (string ControlName, int ExpectedResolutionCount)[] chromeControls =
            [
                ("ActionClusterView.cs", 2),
                ("FloatingActionButtonView.cs", 1),
                ("NavigationBarView.cs", 2),
                ("ScreenHeaderView.cs", 3),
                ("SettingsActionHeaderCardView.cs", 4),
            ];
            (string ControlName, int ExpectedResolutionCount)[] viewerControls =
            [
                ("DarkViewerSurfaceView.cs", 1),
                ("DocumentViewerBodyView.cs", 1),
                ("TextDocumentContentView.cs", 1),
                ("ViewerInfoHeaderView.cs", 3),
                ("ViewerPlayOverlayView.cs", 2),
                ("ViewerOverlayActionButtonView.cs", 1),
            ];

            foreach (string filePath in Directory.EnumerateFiles(controlsPath, "*.cs"))
            {
                string fileName = Path.GetFileName(filePath);
                string control = File.ReadAllText(filePath);

                if (string.Equals(fileName, "MaterialResources.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.DoesNotContain("private static string ResolveStyleResourceKey", control, StringComparison.Ordinal);
            }

            foreach (string controlName in baseContainerControls)
            {
                string control = File.ReadAllText(Path.Combine(controlsPath, controlName));

                Assert.Contains("MaterialResources.ResolveStyleResourceKey(", control, StringComparison.Ordinal);
                Assert.DoesNotContain("string.IsNullOrWhiteSpace(", control, StringComparison.Ordinal);
            }

            foreach ((string controlName, int expectedResolutionCount) in statusFeedbackControls)
            {
                string control = File.ReadAllText(Path.Combine(controlsPath, controlName));

                Assert.Equal(
                    expectedResolutionCount,
                    CountOccurrences(control, "MaterialResources.ResolveStyleResourceKey("));
                Assert.DoesNotContain("StyleResourceKey = string.IsNullOrWhiteSpace(", control, StringComparison.Ordinal);
            }

            foreach ((string controlName, int expectedResolutionCount) in fileItemControls)
            {
                string control = File.ReadAllText(Path.Combine(controlsPath, controlName));

                Assert.Equal(
                    expectedResolutionCount,
                    CountOccurrences(control, "MaterialResources.ResolveStyleResourceKey("));
                Assert.DoesNotContain("StyleResourceKey = string.IsNullOrWhiteSpace(", control, StringComparison.Ordinal);
            }

            foreach ((string controlName, int expectedResolutionCount) in actionMetadataControls)
            {
                string control = File.ReadAllText(Path.Combine(controlsPath, controlName));

                Assert.Equal(
                    expectedResolutionCount,
                    CountOccurrences(control, "MaterialResources.ResolveStyleResourceKey("));
                Assert.DoesNotContain("StyleResourceKey = string.IsNullOrWhiteSpace(", control, StringComparison.Ordinal);
            }

            foreach ((string controlName, int expectedResolutionCount) in chromeControls)
            {
                string control = File.ReadAllText(Path.Combine(controlsPath, controlName));

                Assert.Equal(
                    expectedResolutionCount,
                    CountOccurrences(control, "MaterialResources.ResolveStyleResourceKey("));
                Assert.DoesNotContain("StyleResourceKey = string.IsNullOrWhiteSpace(", control, StringComparison.Ordinal);
            }

            foreach ((string controlName, int expectedResolutionCount) in viewerControls)
            {
                string control = File.ReadAllText(Path.Combine(controlsPath, controlName));

                Assert.Equal(
                    expectedResolutionCount,
                    CountOccurrences(control, "MaterialResources.ResolveStyleResourceKey("));
                Assert.DoesNotContain("StyleResourceKey = string.IsNullOrWhiteSpace(", control, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Pressable_chrome_controls_animate_pressed_backgrounds()
        {
            string materialMotion = LoadText(Path.Combine(ControlsDirectoryPath, "MaterialMotion.cs"));
            string[] controlPaths =
            [
                Path.Combine(ControlsDirectoryPath, "ActionSheetItemView.cs"),
                Path.Combine(ControlsDirectoryPath, "FilledButton.cs"),
                Path.Combine(ControlsDirectoryPath, "IconButton.cs"),
                Path.Combine(ControlsDirectoryPath, "InitialsButton.cs"),
                Path.Combine(ControlsDirectoryPath, "NavigationBarItem.cs"),
                Path.Combine(ControlsDirectoryPath, "TextAction.cs"),
            ];

            Assert.Contains("public static void UpdateBackgroundColor(", materialMotion, StringComparison.Ordinal);

            foreach (string controlPath in controlPaths)
            {
                string control = LoadText(controlPath);

                Assert.Contains("BackgroundAnimationName = \"M3", control, StringComparison.Ordinal);
                if (controlPath.EndsWith("ActionSheetItemView.cs", StringComparison.Ordinal))
                {
                    Assert.Contains("UpdateVisualState(animateBackground: true", control, StringComparison.Ordinal);
                    Assert.Contains("UpdateVisualState(animateBackground: false", control, StringComparison.Ordinal);
                }
                else
                {
                    Assert.Contains("UpdateVisualState(true)", control, StringComparison.Ordinal);
                    Assert.Contains("UpdateVisualState(false)", control, StringComparison.Ordinal);
                }

                Assert.Contains("MaterialMotion.UpdateBackgroundColor(", control, StringComparison.Ordinal);
                Assert.Contains("IsPressed ? PressInDuration : PressOutDuration", control, StringComparison.Ordinal);
                Assert.DoesNotContain("_container.BackgroundColor = IsPressed", control, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Icon_button_animates_chrome_state_without_snap_assignments()
        {
            string iconButton = LoadText(Path.Combine(ControlsDirectoryPath, "IconButton.cs"));

            Assert.Contains("BackgroundAnimationName = \"M3IconButtonBackground\"", iconButton, StringComparison.Ordinal);
            Assert.Contains("BorderColorAnimationName = \"M3IconButtonBorderColor\"", iconButton, StringComparison.Ordinal);
            Assert.Contains("OpacityAnimationName = \"M3IconButtonOpacity\"", iconButton, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateBackgroundColor(", iconButton, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateColor(", iconButton, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", iconButton, StringComparison.Ordinal);
            Assert.Contains("bool shouldAnimate = animateState && _hasAppliedVisualState", iconButton, StringComparison.Ordinal);
            Assert.Contains("IsPressed ? PressInDuration : PressOutDuration", iconButton, StringComparison.Ordinal);
            Assert.Contains("ResolveCurrentBorderColor()", iconButton, StringComparison.Ordinal);
            Assert.DoesNotContain(
                $"{Environment.NewLine}            Opacity = ResolvePressableOpacity(ButtonOpacity);",
                iconButton,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                $"{Environment.NewLine}            _container.Stroke = new SolidColorBrush(BorderColor);",
                iconButton,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Pressable_chrome_controls_animate_opacity_and_outline_state()
        {
            string[] controlPaths =
            [
                Path.Combine(ControlsDirectoryPath, "FilledButton.cs"),
                Path.Combine(ControlsDirectoryPath, "InitialsButton.cs"),
                Path.Combine(ControlsDirectoryPath, "NavigationBarItem.cs"),
            ];

            foreach (string controlPath in controlPaths)
            {
                string control = LoadText(controlPath);

                Assert.Contains("BorderColorAnimationName = \"M3", control, StringComparison.Ordinal);
                Assert.Contains("OpacityAnimationName = \"M3", control, StringComparison.Ordinal);
                Assert.Contains("MaterialMotion.UpdateColor(", control, StringComparison.Ordinal);
                Assert.Contains("MaterialMotion.UpdateDouble(", control, StringComparison.Ordinal);
                Assert.Contains("bool shouldAnimate = animateState && _hasAppliedVisualState", control, StringComparison.Ordinal);
                Assert.Contains("IsPressed ? PressInDuration : PressOutDuration", control, StringComparison.Ordinal);
                Assert.Contains("ResolveCurrentBorderColor()", control, StringComparison.Ordinal);
                Assert.DoesNotContain(
                    $"{Environment.NewLine}            Opacity = ResolvePressableOpacity(1);",
                    control,
                    StringComparison.Ordinal);
                Assert.DoesNotContain(
                    $"{Environment.NewLine}            Opacity = ResolvePressableOpacity(ButtonOpacity);",
                    control,
                    StringComparison.Ordinal);
                Assert.DoesNotContain(
                    "_container.Stroke = new SolidColorBrush(canPress ? BorderColor : DisabledBorderColor);",
                    control,
                    StringComparison.Ordinal);
                Assert.DoesNotContain(
                    $"{Environment.NewLine}            _container.Stroke = new SolidColorBrush(BorderColor);",
                    control,
                    StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Text_action_animates_opacity_state_without_snap_assignment()
        {
            string textAction = LoadText(Path.Combine(ControlsDirectoryPath, "TextAction.cs"));

            Assert.Contains("OpacityAnimationName = \"M3TextActionOpacity\"", textAction, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", textAction, StringComparison.Ordinal);
            Assert.Contains("bool shouldAnimate = animateState && _hasAppliedVisualState", textAction, StringComparison.Ordinal);
            Assert.Contains("IsPressed ? PressInDuration : PressOutDuration", textAction, StringComparison.Ordinal);
            Assert.DoesNotContain(
                $"{Environment.NewLine}            Opacity = ResolvePressableOpacity(1);",
                textAction,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Shared_pressable_labels_animate_text_color_state()
        {
            string materialMotion = LoadText(Path.Combine(ControlsDirectoryPath, "MaterialMotion.cs"));
            string[] controlPaths =
            [
                Path.Combine(ControlsDirectoryPath, "ActionSheetItemView.cs"),
                Path.Combine(ControlsDirectoryPath, "FilledButton.cs"),
                Path.Combine(ControlsDirectoryPath, "InitialsButton.cs"),
                Path.Combine(ControlsDirectoryPath, "NavigationBarItem.cs"),
                Path.Combine(ControlsDirectoryPath, "TextAction.cs"),
            ];

            Assert.Contains("public static void UpdateTextColor(", materialMotion, StringComparison.Ordinal);
            Assert.Contains("Color? currentTextColor = label.TextColor;", materialMotion, StringComparison.Ordinal);
            Assert.Contains("color => label.TextColor = color", materialMotion, StringComparison.Ordinal);

            foreach (string controlPath in controlPaths)
            {
                string control = LoadText(controlPath);

                Assert.Contains("LabelTextColorAnimationName = \"M3", control, StringComparison.Ordinal);
                Assert.Contains("MaterialMotion.UpdateTextColor(", control, StringComparison.Ordinal);
                Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", control, StringComparison.Ordinal);
                Assert.DoesNotContain(
                    $"{Environment.NewLine}            _label.TextColor =",
                    control,
                    StringComparison.Ordinal);
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
            XDocument styles = LoadResourceDictionary(StylesResourcePath);
            XDocument type = LoadResourceDictionary(TypeResourcePath);
            IReadOnlyDictionary<string, string> localCopyChipSetters =
                GetStyleSetters(styles, "M3LocalCopyChip");
            IReadOnlyDictionary<string, string> localCopyChipLabelSetters =
                GetStyleSetters(type, "M3LocalCopyChipLabel");

            Assert.DoesNotContain("<controls:ChipView", mainPage, StringComparison.Ordinal);
            Assert.Contains("new ChipView", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultTrailingChipStyleResourceKey = \"M3NeutralChip\"", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("new ChipView", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultLocalChipStyleResourceKey = \"M3LocalCopyChip\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultLocalChipLabelStyleResourceKey = \"M3LocalCopyChipLabel\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultOfflineChipStyleResourceKey = \"M3FileAttentionChip\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightPrimary}, Dark={StaticResource M3DarkPrimary}}",
                localCopyChipSetters["Stroke"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightPrimary}, Dark={StaticResource M3DarkPrimary}}",
                localCopyChipLabelSetters["TextColor"]);
            Assert.DoesNotContain("M3AccentChipLabel", type.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("M3AccentOutlineChip", styles.ToString() + type.ToString() + fileTileMetadataView, StringComparison.Ordinal);
            Assert.DoesNotContain("M3Accent", styles.ToString() + type, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3LocalCopyChip}\"", mainPage, StringComparison.Ordinal);
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
        public void Material_chip_collapses_empty_text_with_status_motion()
        {
            string chipView = LoadText(Path.Combine(ControlsDirectoryPath, "ChipView.cs"));
            string interaction = LoadText(InteractionResourcePath);

            Assert.Contains("ChipOpacityAnimationName = \"M3ChipOpacity\"", chipView, StringComparison.Ordinal);
            Assert.Contains("propertyChanged: OnTextPropertyChanged", chipView, StringComparison.Ordinal);
            Assert.Contains("UpdateVisualState(animateTextVisibility: true)", chipView, StringComparison.Ordinal);
            Assert.Contains("UpdateVisualState(animateTextVisibility: false)", chipView, StringComparison.Ordinal);
            Assert.Contains("UpdateTextVisibility(text, animateTextVisibility)", chipView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", chipView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", chipView, StringComparison.Ordinal);
            Assert.Contains("CompleteTextVisibility", chipView, StringComparison.Ordinal);
            Assert.Contains("ShouldDeferHiddenTextUpdate", chipView, StringComparison.Ordinal);
            Assert.Contains("!string.IsNullOrWhiteSpace(text)", chipView, StringComparison.Ordinal);
            Assert.Contains("_chip.IsVisible = false", chipView, StringComparison.Ordinal);
            Assert.Contains("_label.Text = text;", chipView, StringComparison.Ordinal);
            Assert.Contains("_label.Text = Text ?? string.Empty;", chipView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_file_tile_metadata_uses_reusable_material_control()
        {
            string mainPage = LoadText(MainPagePath);
            string fileTileEntryCardView = LoadText(FileTileEntryCardViewPath);
            string fileTileMetadataView = LoadText(FileTileMetadataViewPath);
            string interaction = LoadText(InteractionResourcePath);
            string wrappedItemsView = LoadText(WrappedItemsViewPath);
            string materialAnimatedContentView = LoadText(Path.Combine(ControlsDirectoryPath, "MaterialAnimatedContentView.cs"));

            Assert.Contains("<controls:WrappedItemsView IsVisible=\"{Binding Display.IsFileTileViewVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("LayoutStyleResourceKey=\"M3FileTileWrapLayout\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:WrappedItemsView.ItemTemplate>", mainPage, StringComparison.Ordinal);
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
            Assert.Contains("LocalCopyChipOpacityAnimationName = \"M3FileTileLocalChipOpacity\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("OfflineAttentionChipOpacityAnimationName = \"M3FileTileOfflineChipOpacity\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("OnLocalCopyChipVisibilityPropertyChanged", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("OnOfflineAttentionChipVisibilityPropertyChanged", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("CompleteLocalCopyChipVisibility", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("CompleteOfflineAttentionChipVisibility", fileTileMetadataView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.Contains("public class WrappedItemsView", wrappedItemsView, StringComparison.Ordinal);
            Assert.Contains("WrappedItemsView : MaterialAnimatedContentView", wrappedItemsView, StringComparison.Ordinal);
            Assert.Contains("DefaultLayoutStyleResourceKey = \"M3FileTileWrapLayout\"", wrappedItemsView, StringComparison.Ordinal);
            Assert.Contains("public abstract class MaterialAnimatedContentView : ContentView", materialAnimatedContentView, StringComparison.Ordinal);
            Assert.Contains("AppearanceDurationProperty", materialAnimatedContentView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionContentEnterDuration\")", materialAnimatedContentView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", materialAnimatedContentView, StringComparison.Ordinal);
            Assert.Contains("Opacity = MaterialMotion.Value(\"M3MotionHiddenOpacity\")", materialAnimatedContentView, StringComparison.Ordinal);
            Assert.Contains("new FlexLayout()", wrappedItemsView, StringComparison.Ordinal);
            Assert.Contains("BindableLayout.SetItemsSource(_layout, ItemsSource)", wrappedItemsView, StringComparison.Ordinal);
            Assert.Contains("BindableLayout.SetItemTemplate(_layout, ItemTemplate)", wrappedItemsView, StringComparison.Ordinal);
            Assert.DoesNotContain("<FlexLayout IsVisible=\"{Binding Display.IsFileTileViewVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("BindableLayout.ItemsSource", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileSlotGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileContentGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<RowDefinition Height=\"{Binding Source={x:Reference RootPage}, Path=FileTilePreviewHeight}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileTextStack}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileMetadataGrid}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ChipStyleResourceKey=\"M3LocalCopyChip\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ChipStyleResourceKey=\"M3FileAttentionChip\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("_localCopyChip.IsVisible = IsLocalCopyVisible", fileTileMetadataView, StringComparison.Ordinal);
            Assert.DoesNotContain("_offlineAttentionChip.IsVisible = IsOfflineAttentionVisible", fileTileMetadataView, StringComparison.Ordinal);
        }

        [Fact]
        public void Collection_screens_use_reusable_material_collection_control()
        {
            string fileVersionHistoryPage = LoadText(FileVersionHistoryPagePath);
            string materialCollectionView = LoadText(MaterialCollectionViewPath);
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string recentFilesPage = LoadText(RecentFilesPagePath);
            string styles = LoadText(StylesResourcePath);
            string trashPage = LoadText(TrashPagePath);

            string combinedPages = fileVersionHistoryPage
                + pdfViewerPage
                + recentFilesPage
                + trashPage;

            string[] pagePaths =
            [
                FileVersionHistoryPagePath,
                PdfViewerPagePath,
                RecentFilesPagePath,
                TrashPagePath,
            ];

            Assert.Equal(5, CountOccurrences(combinedPages, "<controls:MaterialCollectionView "));
            Assert.Equal(5, CountOccurrences(combinedPages, "<controls:MaterialCollectionView.ItemTemplate>"));
            Assert.Contains("ItemsLayout=\"{StaticResource M3VerticalCardListItemsLayout}\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("ItemsLayout=\"{StaticResource M3VerticalCardListItemsLayout}\"", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("ItemsLayout=\"{StaticResource M3TrashTileItemsLayout}\"", trashPage, StringComparison.Ordinal);
            Assert.Contains("CollectionStyleResourceKey=\"M3DocumentViewerCollection\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("ItemSizingStrategy=\"MeasureAllItems\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("public class MaterialCollectionView", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("MaterialCollectionView : MaterialAnimatedContentView", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("DefaultCollectionStyleResourceKey = \"M3MaterialCollectionView\"", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("typeof(IItemsLayout)", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("LinearItemsLayout.Vertical", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("ItemSizingStrategy.MeasureFirstItem", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("SelectionMode.None", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("_collection.SetDynamicResource(StyleProperty, collectionStyleResourceKey)", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("_collection.ItemsSource = ItemsSource", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("_collection.ItemTemplate = ItemTemplate", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("_collection.ItemsLayout = ItemsLayout", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("_collection.SelectionMode = SelectionMode", materialCollectionView, StringComparison.Ordinal);
            Assert.Contains("x:Key=\"M3MaterialCollectionView\"", styles, StringComparison.Ordinal);
            Assert.Contains("x:Key=\"M3DocumentViewerCollection\" BasedOn=\"{StaticResource M3MaterialCollectionView}\"", styles, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionContentEnterDuration\">140</x:Int32>", LoadText(InteractionResourcePath), StringComparison.Ordinal);

            foreach (string pagePath in pagePaths)
            {
                string page = LoadText(pagePath);

                Assert.DoesNotContain("<CollectionView", page, StringComparison.Ordinal);
                Assert.DoesNotContain("SelectionMode=\"None\"", page, StringComparison.Ordinal);
                Assert.DoesNotContain("<CollectionView.ItemTemplate>", page, StringComparison.Ordinal);
            }
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
            string fileEntryTextView = LoadText(FileEntryTextViewPath);
            string interaction = LoadText(InteractionResourcePath);
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
            Assert.Contains("BodyContentOpacityAnimationName = \"M3MetadataCardBodyContentOpacity\"", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("OnBodyContentVisibilityPropertyChanged", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("CompleteBodyContentVisibility", metadataCardView, StringComparison.Ordinal);
            Assert.DoesNotContain("_bodyContentHost.IsVisible = bodyContent is not null", metadataCardView, StringComparison.Ordinal);
            Assert.Contains("new FileEntryTextView", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("new ChipView", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3MetadataCardGrid\"", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("DefaultTrailingChipStyleResourceKey = \"M3NeutralChip\"", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("TrailingChipOpacityAnimationName = \"M3MetadataCardTrailingChipOpacity\"", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("TrailingTextProperty", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnTrailingChipVisibilityPropertyChanged", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteTrailingChipVisibility", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("IsTrailingChipActuallyVisible", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("!string.IsNullOrWhiteSpace(trailingText)", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain("_trailingChip.IsVisible = IsTrailingTextVisible", metadataCardHeaderView, StringComparison.Ordinal);
            Assert.Contains("DetailOpacityAnimationName = \"M3FileEntryDetailOpacity\"", fileEntryTextView, StringComparison.Ordinal);
            Assert.Contains("OnDetailVisiblePropertyChanged", fileEntryTextView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", fileEntryTextView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", fileEntryTextView, StringComparison.Ordinal);
            Assert.Contains("CompleteDetailVisibility", fileEntryTextView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_detailLabel.IsVisible = IsDetailVisible", fileEntryTextView, StringComparison.Ordinal);
            Assert.Contains("x:Key=\"M3MetadataCardBodyStack\"", styles, StringComparison.Ordinal);
            Assert.Contains("public class MetadataCardBodyView", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3MetadataCardBodyStack\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultInlineGridStyleResourceKey = \"M3InlineMetadataGrid\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("new LinearProgressView", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("ProgressOpacityAnimationName = \"M3MetadataCardProgressOpacity\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("InlineMetadataOpacityAnimationName = \"M3MetadataCardInlineMetadataOpacity\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("PrimaryTextOpacityAnimationName = \"M3MetadataCardPrimaryTextOpacity\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("SecondaryTextOpacityAnimationName = \"M3MetadataCardSecondaryTextOpacity\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("ErrorTextOpacityAnimationName = \"M3MetadataCardErrorTextOpacity\"", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", metadataCardBodyView, StringComparison.Ordinal);
            Assert.Contains("CompleteElementVisibility", metadataCardBodyView, StringComparison.Ordinal);
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

            Assert.DoesNotContain("_progress.IsVisible = IsProgressVisible", metadataCardBodyView, StringComparison.Ordinal);
            Assert.DoesNotContain("_inlineGrid.IsVisible = IsInlineMetadataVisible", metadataCardBodyView, StringComparison.Ordinal);
            Assert.DoesNotContain("_primaryText.IsVisible = IsPrimaryTextVisible", metadataCardBodyView, StringComparison.Ordinal);
            Assert.DoesNotContain("_secondaryText.IsVisible = IsSecondaryTextVisible", metadataCardBodyView, StringComparison.Ordinal);
            Assert.DoesNotContain("_errorText.IsVisible = IsErrorTextVisible", metadataCardBodyView, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_feed_lists_use_reusable_stacked_items_control()
        {
            string activityFeedPage = LoadText(ActivityFeedPagePath);
            string captureDestinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string captureInboxPage = LoadText(CaptureInboxPagePath);
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string stackedItemsView = LoadText(StackedItemsViewPath);
            string syncSettingsPage = LoadText(SyncSettingsPagePath);
            string transfersPage = LoadText(TransfersPagePath);

            string combinedPages = activityFeedPage
                + captureDestinationPickerPage
                + captureInboxPage
                + notificationSettingsPage
                + securitySettingsPage
                + syncSettingsPage
                + transfersPage;

            Assert.Equal(8, CountOccurrences(combinedPages, "<controls:StackedItemsView "));
            Assert.Equal(4, CountOccurrences(combinedPages, "IsVisible=\"{Binding IsListVisible}\""));
            Assert.Contains("ItemsSource=\"{Binding Items}\"", activityFeedPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding Items}\"", transfersPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding Items}\"", captureInboxPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding Folders}\"", captureDestinationPickerPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding Roots}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding PermissionLedgerItems}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding AccountSessions}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding RemotePushPreferences}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:StackedItemsView.ItemTemplate>", combinedPages, StringComparison.Ordinal);
            Assert.Contains("public class StackedItemsView", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("using System.Collections;", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("StackedItemsView : MaterialAnimatedContentView", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3SettingsSectionStack\"", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("ItemsSourceProperty", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("ItemTemplateProperty", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("StackStyleResourceKeyProperty", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("_stack.SetDynamicResource(StyleProperty, stackStyleResourceKey)", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("BindableLayout.SetItemsSource(_stack, ItemsSource)", stackedItemsView, StringComparison.Ordinal);
            Assert.Contains("BindableLayout.SetItemTemplate(_stack, ItemTemplate)", stackedItemsView, StringComparison.Ordinal);
            Assert.DoesNotContain("BindableLayout.ItemsSource", combinedPages, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3SettingsSectionStack}\"", combinedPages, StringComparison.Ordinal);
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
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains("EntryActionsOpacityAnimationName = \"M3TrashEntryActionsOpacity\"", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("UpdateEntryActionsVisibility", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("CompleteEntryActionsVisibility", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", trashEntryCardViewBase, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("M3RowActionCluster", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:ActionClusterView Grid.Row=\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Delete}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconButton IconData=\"{x:Static controls:IconPathData.Reset}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,Auto\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,Auto,Auto\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("actionCluster.IsVisible = IsEntryActionsVisible", trashEntryCardViewBase, StringComparison.Ordinal);
        }

        [Fact]
        public void Sync_root_actions_use_reusable_material_control()
        {
            string syncSettingsPage = LoadText(SyncSettingsPagePath);
            string stackedContentView = LoadText(StackedContentViewPath);
            string settingsInfoItemView = LoadText(SettingsInfoItemViewPath);

            Assert.Contains("public class StackedContentView", stackedContentView, StringComparison.Ordinal);
            Assert.Contains("<controls:StackedContentView StackStyleResourceKey=\"M3SettingsDenseStack\">", syncSettingsPage, StringComparison.Ordinal);
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
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3SettingsDenseStack}\">", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding RunNowActionText}\"", syncSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding StopSyncActionText}\"", syncSettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Storage_bucket_rows_use_reusable_material_control()
        {
            string storagePage = LoadText(StoragePagePath);
            string storageBucketItemView = LoadText(StorageBucketItemViewPath);
            string interaction = LoadText(InteractionResourcePath);

            Assert.Equal(2, CountOccurrences(storagePage, "<controls:StackedItemsView ItemsSource=\"{Binding "));
            Assert.Equal(2, CountOccurrences(storagePage, "StackStyleResourceKey=\"M3SettingsItemListStack\""));
            Assert.Equal(2, CountOccurrences(storagePage, "<controls:StorageBucketItemView"));
            Assert.Contains("ItemsSource=\"{Binding OnDeviceBuckets}\"", storagePage, StringComparison.Ordinal);
            Assert.Contains("ItemsSource=\"{Binding StorageBudgetBuckets}\"", storagePage, StringComparison.Ordinal);
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
            Assert.Contains(
                "PrimaryMetricTextOpacityAnimationName = \"M3StorageBucketPrimaryMetricOpacity\"",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "ProgressOpacityAnimationName = \"M3StorageBucketProgressOpacity\"",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecondaryMetricTextOpacityAnimationName = \"M3StorageBucketSecondaryMetricOpacity\"",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnPrimaryMetricTextVisibilityPropertyChanged",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnProgressVisibilityPropertyChanged",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnSecondaryMetricTextVisibilityPropertyChanged",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains(
                "MaterialResources.Get<int>(\"M3MotionStatusDuration\")",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.Contains("CompletePrimaryMetricTextVisibility", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteProgressVisibility", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteSecondaryMetricTextVisibility", storageBucketItemView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("x:DataType=\"services:CottonOnDeviceStorageBucketSnapshot\">\n                            <Grid", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("x:DataType=\"services:CottonStorageBudgetBucketSnapshot\">\n                            <Grid", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:LinearProgressView Grid.Row=\"2\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3CardFileThumbnailFrame}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("BindableLayout.ItemsSource", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3SettingsItemListStack}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("_progress.IsVisible = IsProgressVisible", storageBucketItemView, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_primaryMetricText.IsVisible = !string.IsNullOrWhiteSpace(primaryMetricText)",
                storageBucketItemView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_secondaryMetricText.IsVisible = !string.IsNullOrWhiteSpace(secondaryMetricText)",
                storageBucketItemView,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Storage_section_headers_use_reusable_material_control()
        {
            string storagePage = LoadText(StoragePagePath);
            string settingsSectionHeaderView = LoadText(SettingsSectionHeaderViewPath);
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains(
                "ProgressOpacityAnimationName = \"M3SettingsSectionProgressOpacity\"",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains(
                "OnProgressVisibilityPropertyChanged",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains(
                "MaterialResources.Get<int>(\"M3MotionStatusDuration\")",
                settingsSectionHeaderView,
                StringComparison.Ordinal);
            Assert.Contains("CompleteProgressVisibility", settingsSectionHeaderView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("Grid.RowSpan=\"2\"\n                                        IconData=\"{x:Static controls:IconPathData.Cloud}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding CloudQuotaTitle}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"Free up storage\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding OnDeviceSummaryText}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding StorageBudgetSummaryText}\"", storagePage, StringComparison.Ordinal);
            Assert.DoesNotContain("_progress.IsVisible = IsProgressVisible", settingsSectionHeaderView, StringComparison.Ordinal);
        }

        [Fact]
        public void Diagnostics_rows_use_reusable_material_control()
        {
            string diagnosticsPage = LoadText(DiagnosticsPagePath);
            string diagnosticsItemView = LoadText(DiagnosticsItemViewPath);
            string stackedContentView = LoadText(StackedContentViewPath);

            Assert.Contains("<controls:StackedItemsView ItemsSource=\"{Binding Sections}\"\n                                       StackStyleResourceKey=\"M3DiagnosticsSectionListStack\">", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:StackedContentView>", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:StackedItemsView ItemsSource=\"{Binding Items}\"\n                                                           StackStyleResourceKey=\"M3DiagnosticsItemListStack\">", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:SettingsSectionHeaderView Title=\"{Binding Title}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("IsTapEnabled=\"False\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DiagnosticsItemView LabelText=\"{Binding Label}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("ValueText=\"{Binding Value}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.Contains("public class StackedContentView", stackedContentView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3SettingsSectionStack\"", stackedContentView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _stack.Children", stackedContentView, StringComparison.Ordinal);
            Assert.Contains("_stack.SetDynamicResource(StyleProperty, stackStyleResourceKey)", stackedContentView, StringComparison.Ordinal);
            Assert.Contains("public class DiagnosticsItemView", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3DiagnosticsItemGrid\"", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultLabelTextStyleResourceKey = \"M3CardSupporting\"", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("DefaultValueTextStyleResourceKey = \"M3CardSupportingPrimaryBlock\"", diagnosticsItemView, StringComparison.Ordinal);
            Assert.Contains("M3DiagnosticsLabelColumnWidth", diagnosticsItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("BindableLayout.ItemsSource", diagnosticsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3DiagnosticsSectionListStack}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3DiagnosticsItemListStack}\"", diagnosticsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3SettingsSectionStack}\">", diagnosticsPage, StringComparison.Ordinal);
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
        public void Viewer_pages_use_reusable_material_page_shells()
        {
            string textViewerPage = LoadText(TextViewerPagePath);
            string textViewerPageCodeBehind = LoadText(TextViewerPageCodeBehindPath);
            string imageViewerPage = LoadText(ImageViewerPagePath);
            string imageViewerPageCodeBehind = LoadText(ImageViewerPageCodeBehindPath);
            string mediaViewerPage = LoadText(MediaViewerPagePath);
            string mediaViewerPageCodeBehind = LoadText(MediaViewerPageCodeBehindPath);
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string pdfViewerPageCodeBehind = LoadText(PdfViewerPageCodeBehindPath);
            string darkViewerPage = LoadText(DarkViewerPagePath);
            string documentViewerPage = LoadText(DocumentViewerPagePath);
            string combinedViewerPages = textViewerPage
                + imageViewerPage
                + mediaViewerPage
                + pdfViewerPage;

            Assert.Contains("<controls:DocumentViewerPage xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DocumentViewerPage xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DarkViewerPage xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DarkViewerPage xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("public partial class TextViewerPage : DocumentViewerPage", textViewerPageCodeBehind, StringComparison.Ordinal);
            Assert.Contains("public partial class PdfViewerPage : DocumentViewerPage", pdfViewerPageCodeBehind, StringComparison.Ordinal);
            Assert.Contains("public partial class ImageViewerPage : DarkViewerPage", imageViewerPageCodeBehind, StringComparison.Ordinal);
            Assert.Contains("public partial class MediaViewerPage : DarkViewerPage", mediaViewerPageCodeBehind, StringComparison.Ordinal);
            Assert.Contains("public class DocumentViewerPage : ContentPage", documentViewerPage, StringComparison.Ordinal);
            Assert.Contains("DefaultPageStyleResourceKey = \"M3DocumentViewerPage\"", documentViewerPage, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultPageStyleResourceKey)", documentViewerPage, StringComparison.Ordinal);
            Assert.Contains("public class DarkViewerPage : ContentPage", darkViewerPage, StringComparison.Ordinal);
            Assert.Contains("DefaultPageStyleResourceKey = \"M3DarkViewerPage\"", darkViewerPage, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultPageStyleResourceKey)", darkViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<ContentPage xmlns=\"http://schemas.microsoft.com/dotnet/2021/maui\"", combinedViewerPages, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3DocumentViewerPage}\"", combinedViewerPages, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3DarkViewerPage}\"", combinedViewerPages, StringComparison.Ordinal);
        }

        [Fact]
        public void Document_viewer_headers_use_reusable_material_control()
        {
            string textViewerPage = LoadText(TextViewerPagePath);
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string documentViewerBodyView = LoadText(DocumentViewerBodyViewPath);
            string viewerInfoHeaderView = LoadText(ViewerInfoHeaderViewPath);
            string interaction = LoadText(InteractionResourcePath);

            Assert.Contains("<controls:ScreenShellView>", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenShellView GridStyleResourceKey=\"M3DocumentViewerSurface\">", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DocumentViewerBodyView Grid.Row=\"1\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("GridStyleResourceKey=\"M3TextViewerContentGrid\"", textViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DocumentViewerBodyView Grid.Row=\"1\">", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:LayeredContentView Grid.Row=\"1\"\n                                         IsVisible=\"{Binding IsEmptyVisible}\"\n                                         GridStyleResourceKey=\"M3PdfEmptyStateLayer\">", pdfViewerPage, StringComparison.Ordinal);
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
            Assert.Contains("StatusOpacityAnimationName = \"M3ViewerInfoStatusOpacity\"", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnStatusVisiblePropertyChanged", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteStatusVisibility", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_status.IsVisible = IsStatusVisible", viewerInfoHeaderView, StringComparison.Ordinal);
            Assert.Contains("public class DocumentViewerBodyView", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3DocumentViewerSurface\"", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto })", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star })", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _grid.Children", documentViewerBodyView, StringComparison.Ordinal);
            Assert.Contains("_grid.SetDynamicResource(StyleProperty, gridStyleResourceKey)", documentViewerBodyView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\">", textViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\"\n          Style=\"{StaticResource M3DocumentViewerSurface}\">", pdfViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"\n                  IsVisible=\"{Binding IsEmptyVisible}\"\n                  Style=\"{StaticResource M3PdfEmptyStateLayer}\">", pdfViewerPage, StringComparison.Ordinal);
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
        public void Pdf_preview_pages_use_reusable_material_control()
        {
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string pdfPreviewPageView = LoadText(PdfPreviewPageViewPath);

            Assert.Contains("<controls:PdfPreviewPageView ImageSource=\"{Binding ImageSource}\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("DisplayHeight=\"{Binding DisplayHeight}\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.Contains("public class PdfPreviewPageView", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("DefaultContainerStyleResourceKey = \"M3PdfPageContainerGrid\"", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("DefaultCardStyleResourceKey = \"M3PdfPageSurface\"", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("DefaultImageStyleResourceKey = \"M3PdfPageImage\"", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("new Image()", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("_container.SetDynamicResource(StyleProperty, containerStyleResourceKey)", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("_card.CardStyleResourceKey = cardStyleResourceKey", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("_image.SetDynamicResource(StyleProperty, imageStyleResourceKey)", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("_image.HeightRequest = DisplayHeight", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Style=\"{StaticResource M3PdfPageContainerGrid}\">", pdfViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Image Source=\"{Binding ImageSource}\"", pdfViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3PdfPageImage}\"", pdfViewerPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Dark_viewer_status_overlays_use_reusable_material_control()
        {
            string imageViewerPage = LoadText(ImageViewerPagePath);
            string mediaViewerPage = LoadText(MediaViewerPagePath);
            string darkViewerSurfaceView = LoadText(DarkViewerSurfaceViewPath);
            string viewerImageView = LoadText(ViewerImageViewPath);
            string viewerMediaElementView = LoadText(ViewerMediaElementViewPath);
            string viewerOverlayActionButtonView = LoadText(ViewerOverlayActionButtonViewPath);
            string viewerPlayOverlayView = LoadText(ViewerPlayOverlayViewPath);
            string viewerStatusOverlayView = LoadText(ViewerStatusOverlayViewPath);
            string interaction = LoadText(InteractionResourcePath);

            Assert.Contains("<controls:ScreenShellView GridStyleResourceKey=\"M3DarkViewerSurface\">", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenShellView GridStyleResourceKey=\"M3DarkViewerSurface\">", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DarkViewerSurfaceView Grid.Row=\"1\"\n                                        x:Name=\"ImageSurface\">", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:DarkViewerSurfaceView Grid.Row=\"1\">", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerImageView x:Name=\"PreviewImage\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerImageView.GestureRecognizers>", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerMediaElementView x:Name=\"MediaPlayer\" />", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerStatusOverlayView Text=\"{Binding Status}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisible=\"{Binding IsStatusVisible}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("StatusStyleResourceKey=\"M3ViewerOverlayStatusWithTrailingAction\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerOverlayActionButtonView x:Name=\"ResetButton\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding Source={x:Reference RootPage}, Path=ResetImageCommand}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"Reset image\"", imageViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerStatusOverlayView Text=\"{Binding Status}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisible=\"{Binding IsStatusVisible}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ViewerPlayOverlayView x:Name=\"StartOverlay\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("Command=\"{Binding Source={x:Reference RootPage}, Path=PlayMediaCommand}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("SemanticDescription=\"Play media\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.Contains("DefaultStatusStyleResourceKey = \"M3ViewerOverlayStatus\"", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("IsStatusVisibleProperty", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("StatusOpacityAnimationName = \"M3ViewerOverlayStatusOpacity\"", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("OnStatusVisiblePropertyChanged", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("CompleteStatusVisibility", viewerStatusOverlayView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
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
            Assert.Contains("public class ViewerImageView : Image", viewerImageView, StringComparison.Ordinal);
            Assert.Contains("DefaultImageStyleResourceKey = \"M3ViewerImage\"", viewerImageView, StringComparison.Ordinal);
            Assert.Contains("Aspect = Aspect.AspectFit", viewerImageView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultImageStyleResourceKey)", viewerImageView, StringComparison.Ordinal);
            Assert.Contains("public class ViewerMediaElementView : MediaElement", viewerMediaElementView, StringComparison.Ordinal);
            Assert.Contains("DefaultMediaStyleResourceKey = \"M3ViewerMediaElement\"", viewerMediaElementView, StringComparison.Ordinal);
            Assert.Contains("Aspect = Aspect.AspectFit", viewerMediaElementView, StringComparison.Ordinal);
            Assert.Contains("ShouldAutoPlay = false", viewerMediaElementView, StringComparison.Ordinal);
            Assert.Contains("ShouldLoopPlayback = false", viewerMediaElementView, StringComparison.Ordinal);
            Assert.Contains("ShouldShowPlaybackControls = true", viewerMediaElementView, StringComparison.Ordinal);
            Assert.Contains("SetDynamicResource(StyleProperty, DefaultMediaStyleResourceKey)", viewerMediaElementView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\"\n          Style=\"{StaticResource M3DarkViewerSurface}\">", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\"\n          Style=\"{StaticResource M3DarkViewerSurface}\">", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3DarkViewerSurface}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3DarkViewerSurface}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Image x:Name=\"PreviewImage\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<toolkit:MediaElement", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("xmlns:toolkit=", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ViewerImage}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ViewerMediaElement}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Aspect=\"AspectFit\"", imageViewerPage + mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ShouldAutoPlay=\"False\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ShouldLoopPlayback=\"False\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("ShouldShowPlaybackControls=\"True\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid x:Name=\"ImageSurface\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"1\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Status}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding Status}\"", mediaViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("IsVisible=\"{Binding IsStatusVisible}\"", imageViewerPage, StringComparison.Ordinal);
            Assert.DoesNotContain("IsVisible=\"{Binding IsStatusVisible}\"", mediaViewerPage, StringComparison.Ordinal);
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
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains(
                "LeadingIconOpacityAnimationName = \"M3SettingsInfoLeadingIconOpacity\"",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "PrimaryDetailTextOpacityAnimationName = \"M3SettingsInfoPrimaryDetailOpacity\"",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "SecondaryDetailTextOpacityAnimationName = \"M3SettingsInfoSecondaryDetailOpacity\"",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "TertiaryDetailTextOpacityAnimationName = \"M3SettingsInfoTertiaryDetailOpacity\"",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.Contains(
                "TrailingChipOpacityAnimationName = \"M3SettingsInfoTrailingChipOpacity\"",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.Contains("OnLeadingIconVisibilityPropertyChanged", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("OnPrimaryDetailTextVisibilityPropertyChanged", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("OnSecondaryDetailTextVisibilityPropertyChanged", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("OnTertiaryDetailTextVisibilityPropertyChanged", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("OnTrailingChipVisibilityPropertyChanged", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteLeadingIconVisibility", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("CompletePrimaryDetailTextVisibility", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteSecondaryDetailTextVisibility", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteTertiaryDetailTextVisibility", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteTrailingChipVisibility", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("ResolveLeadingIconLayoutVisibility", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("ResolveTrailingChipLayoutVisibility", settingsInfoItemView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_leadingIcon.IsVisible = leadingIconData is not null", settingsInfoItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid ColumnDefinitions=\"Auto,*,Auto\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:IconFrame Grid.RowSpan", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"controls:IconFrame\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("TargetType=\"controls:ChipView\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Grid.Column=\"1\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Grid.Row=\"1\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding AccountSessionsEmptyTitle}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Label Text=\"{Binding AccountSessionsEmptyDetails}\"", securitySettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_primaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(primaryDetailText)",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_secondaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(secondaryDetailText)",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_tertiaryDetailText.IsVisible = !string.IsNullOrWhiteSpace(tertiaryDetailText)",
                settingsInfoItemView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_trailingChip.IsVisible = isTrailingTextVisible",
                settingsInfoItemView,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Settings_summary_headers_use_reusable_material_control()
        {
            string notificationSettingsPage = LoadText(NotificationSettingsPagePath);
            string securitySettingsPage = LoadText(SecuritySettingsPagePath);
            string settingsSummaryHeaderView = LoadText(SettingsSummaryHeaderViewPath);
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains("StatusOpacityAnimationName = \"M3SettingsSummaryStatusOpacity\"", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("DetailOpacityAnimationName = \"M3SettingsSummaryDetailOpacity\"", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnStatusVisiblePropertyChanged", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnDetailVisiblePropertyChanged", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteStatusVisibility", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteDetailVisibility", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_statusLabel.IsVisible = IsStatusVisible", settingsSummaryHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain("_detailLabel.IsVisible = IsDetailVisible", settingsSummaryHeaderView, StringComparison.Ordinal);
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
            string interaction = LoadText(InteractionResourcePath);
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
            Assert.Contains("SupportingTextOpacityAnimationName = \"M3SettingsToggleSupportingTextOpacity\"", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("DetailTextOpacityAnimationName = \"M3SettingsToggleDetailTextOpacity\"", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("LeadingIconOpacityAnimationName = \"M3SettingsToggleLeadingIconOpacity\"", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("OnLeadingIconVisibilityPropertyChanged", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("OnSupportingTextVisibilityPropertyChanged", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("OnDetailTextVisibilityPropertyChanged", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteLeadingIconVisibility", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("ResolveLeadingIconLayoutVisibility", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteSupportingTextVisibility", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("CompleteDetailTextVisibility", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_leadingIcon.IsVisible = isLeadingIconVisible", settingsToggleItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("_supportingText.IsVisible = IsSupportingTextVisible", settingsToggleItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("_detailText.IsVisible = IsDetailTextVisible", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("new Binding(nameof(IsToggled), source: this, mode: BindingMode.TwoWay)", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("_toggleSwitch.SetDynamicResource(StyleProperty, switchStyleResourceKey)", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("private readonly TouchSurfaceView _touchSurface;", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface = new TouchSurfaceView();", settingsToggleItemView, StringComparison.Ordinal);
            Assert.Contains("_touchSurface.TapCommand = IsEnabled ? _toggleCommand : null;", settingsToggleItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("LongPressBehavior", settingsToggleItemView, StringComparison.Ordinal);
            Assert.DoesNotContain("M3ListItemTouchSurface", settingsToggleItemView, StringComparison.Ordinal);
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
            string authSignInPanelView = LoadText(AuthSignInPanelViewPath);
            string captureDestinationPickerPage = LoadText(CaptureDestinationPickerPagePath);
            string diagnosticsPage = LoadText(DiagnosticsPagePath);
            string mainPage = LoadText(MainPagePath);
            string pdfViewerPage = LoadText(PdfViewerPagePath);
            string pdfPreviewPageView = LoadText(PdfPreviewPageViewPath);
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
            Assert.Equal(0, CountOccurrences(mainPage, "<controls:ContentCardView"));
            Assert.Equal(0, CountOccurrences(pdfViewerPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(syncSettingsPage, "<controls:ContentCardView"));
            Assert.Equal(1, CountOccurrences(textViewerPage, "<controls:ContentCardView"));
            Assert.DoesNotContain("<controls:ContentCardView", trashPage, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", pdfPreviewPageView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", settingsActionHeaderCardView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", trashListEntryCardView, StringComparison.Ordinal);
            Assert.Contains("new ContentCardView", trashTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultCardStyleResourceKey = \"M3AuthPanel\"", authSignInPanelView, StringComparison.Ordinal);
            Assert.Contains("CardStyleResourceKey = \"M3FileTileCard\"", fileTileEntryCardView, StringComparison.Ordinal);
            Assert.Contains("DefaultCardStyleResourceKey = \"M3PdfPageSurface\"", pdfPreviewPageView, StringComparison.Ordinal);
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
            string fileEntryActionButtonView = LoadText(Path.Combine(ControlsDirectoryPath, "FileEntryActionButtonView.cs"));
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains("ActionButtonOpacityAnimationName = \"M3FileEntryActionButtonOpacity\"", fileEntryActionButtonView, StringComparison.Ordinal);
            Assert.Contains("OnActionVisibilityPropertyChanged", fileEntryActionButtonView, StringComparison.Ordinal);
            Assert.Contains(
                "nameof(IsActionVisible),\n            typeof(bool),\n            typeof(FileEntryActionButtonView),\n            true,\n            propertyChanged: OnActionVisibilityPropertyChanged);",
                fileEntryActionButtonView,
                StringComparison.Ordinal);
            Assert.Contains(
                "nameof(IsActionEnabled),\n            typeof(bool),\n            typeof(FileEntryActionButtonView),\n            true,\n            propertyChanged: OnVisualPropertyChanged);",
                fileEntryActionButtonView,
                StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", fileEntryActionButtonView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", fileEntryActionButtonView, StringComparison.Ordinal);
            Assert.Contains("CompleteActionButtonVisibility", fileEntryActionButtonView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticProperties.Description=\"{Binding Name, StringFormat='Actions for {0}'}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileActionIconButton}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("_actionButton.IsVisible = IsActionVisible", fileEntryActionButtonView, StringComparison.Ordinal);
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
            string interaction = LoadText(InteractionResourcePath);

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
            Assert.Contains("TrailingChipOpacityAnimationName = \"M3FileListTrailingChipOpacity\"", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("OnTrailingChipVisibilityPropertyChanged", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("CompleteTrailingChipVisibility", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_trailingChip.IsVisible = IsTrailingTextVisible", fileListMetadataView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3FileTileTextStack\"", fileTileMetadataView, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3CardTextStack}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileTileTextStack}\"", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<controls:FileEntryTextView", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_screen_content_grids_use_reusable_material_shell()
        {
            string fileVersionHistoryPage = LoadText(FileVersionHistoryPagePath);
            string layeredContentView = LoadText(LayeredContentViewPath);
            string recentFilesPage = LoadText(RecentFilesPagePath);
            string screenContentGridView = LoadText(ScreenContentGridViewPath);
            string screenShellView = LoadText(ScreenShellViewPath);
            string styles = LoadText(StylesResourcePath);
            string trashPage = LoadText(TrashPagePath);

            Assert.Contains("<controls:ScreenShellView>", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenShellView>", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenShellView>", trashPage, StringComparison.Ordinal);
            Assert.Contains("<controls:LayeredContentView Grid.Row=\"2\">", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("<controls:LayeredContentView Grid.Row=\"4\">", trashPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenContentGridView Grid.Row=\"1\">", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenContentGridView Grid.Row=\"1\">", recentFilesPage, StringComparison.Ordinal);
            Assert.Contains("<controls:ScreenContentGridView Grid.Row=\"1\"\n                                        ExtraAutoRows=\"2\">", trashPage, StringComparison.Ordinal);
            Assert.Contains("public class LayeredContentView", layeredContentView, StringComparison.Ordinal);
            Assert.Contains("GridStyleResourceKeyProperty", layeredContentView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3LayeredContent\"", layeredContentView, StringComparison.Ordinal);
            Assert.Contains("new Grid()", layeredContentView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _grid.Children", layeredContentView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.ResolveStyleResourceKey(", layeredContentView, StringComparison.Ordinal);
            Assert.Contains("_grid.SetDynamicResource(StyleProperty, gridStyleResourceKey)", layeredContentView, StringComparison.Ordinal);
            Assert.Contains("public class ScreenShellView", screenShellView, StringComparison.Ordinal);
            Assert.Contains("GridStyleResourceKeyProperty", screenShellView, StringComparison.Ordinal);
            Assert.Contains("DefaultGridStyleResourceKey = \"M3ScreenShell\"", screenShellView, StringComparison.Ordinal);
            Assert.Contains("new Grid()", screenShellView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto })", screenShellView, StringComparison.Ordinal);
            Assert.Contains("_grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star })", screenShellView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _grid.Children", screenShellView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.ResolveStyleResourceKey(", screenShellView, StringComparison.Ordinal);
            Assert.Contains("_grid.SetDynamicResource(StyleProperty, gridStyleResourceKey)", screenShellView, StringComparison.Ordinal);
            Assert.Contains("x:Key=\"M3ScreenShell\"", styles, StringComparison.Ordinal);
            Assert.Contains("x:Key=\"M3LayeredContent\"", styles, StringComparison.Ordinal);
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
            Assert.DoesNotContain("<Grid Grid.Row=\"2\">", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid Grid.Row=\"4\">", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\">", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\">", recentFilesPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\">", trashPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ScreenContentGrid}\"", fileVersionHistoryPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ScreenContentGrid}\"", recentFilesPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3ScreenContentGrid}\"", trashPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Secondary_screen_scroll_bodies_use_reusable_material_shell()
        {
            string screenShellView = LoadText(ScreenShellViewPath);
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

            Assert.Contains("public class ScreenShellView", screenShellView, StringComparison.Ordinal);
            Assert.Contains("public class ScreenScrollBodyView", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("new ScrollView", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("new VerticalStackLayout", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("DefaultStackStyleResourceKey = \"M3ScreenContentStack\"", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _stack.Children", screenScrollBodyView, StringComparison.Ordinal);

            foreach (string screenPath in scrollBodyScreenPaths)
            {
                string page = LoadText(screenPath);

                Assert.Contains("<controls:ScreenShellView>", page, StringComparison.Ordinal);
                Assert.Contains("<controls:ScreenScrollBodyView Grid.Row=\"1\">", page, StringComparison.Ordinal);
                Assert.DoesNotContain("<Grid RowDefinitions=\"Auto,*\">", page, StringComparison.Ordinal);
                Assert.DoesNotContain("<ScrollView Grid.Row=\"1\">", page, StringComparison.Ordinal);
                Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3ScreenContentStack}\">", page, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Main_screen_scroll_bodies_use_reusable_material_shell()
        {
            string mainPage = LoadText(MainPagePath);
            string screenScrollBodyView = LoadText(ScreenScrollBodyViewPath);

            Assert.Contains("public class ScreenScrollBodyView", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Contains("StackStyleResourceKeyProperty", screenScrollBodyView, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(mainPage, "<controls:ScreenScrollBodyView"));
            Assert.Contains("<controls:ScreenScrollBodyView Grid.Row=\"0\"", mainPage, StringComparison.Ordinal);
            Assert.Equal(2, CountOccurrences(mainPage, "StackStyleResourceKey=\"M3MainContentStack\""));
            Assert.DoesNotContain("<ScrollView", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("<VerticalStackLayout Style=\"{StaticResource M3MainContentStack}\">", mainPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_screen_stacked_sections_use_reusable_material_shell()
        {
            string mainPage = LoadText(MainPagePath);
            string stackedContentView = LoadText(StackedContentViewPath);

            Assert.Contains("public class StackedContentView", stackedContentView, StringComparison.Ordinal);
            Assert.Contains("public IList<IView> Items => _stack.Children", stackedContentView, StringComparison.Ordinal);
            Assert.Equal(3, CountOccurrences(mainPage, "<controls:StackedContentView"));
            Assert.Contains("<controls:StackedContentView IsVisible=\"{Binding Display.IsBrandHeaderVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("StackStyleResourceKey=\"M3AuthShellStack\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:StackedContentView IsVisible=\"{Binding Display.IsProfileVisible}\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("StackStyleResourceKey=\"M3FileBrowserProfileStack\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("<controls:StackedContentView x:Name=\"FileBrowserContent\"", mainPage, StringComparison.Ordinal);
            Assert.Contains("StackStyleResourceKey=\"M3FileBrowserContentStack\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3AuthShellStack}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileBrowserProfileStack}\"", mainPage, StringComparison.Ordinal);
            Assert.DoesNotContain("Style=\"{StaticResource M3FileBrowserContentStack}\"", mainPage, StringComparison.Ordinal);
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
        public void Screen_header_busy_state_uses_material_action_frame()
        {
            string screenHeaderView = LoadText(ScreenHeaderViewPath);
            string interaction = LoadText(InteractionResourcePath);
            XDocument styles = LoadResourceDictionary(StylesResourcePath);

            IReadOnlyDictionary<string, string> busyFrameSetters = GetStyleSetters(styles, "M3ScreenHeaderBusyFrame");
            IReadOnlyDictionary<string, string> busyIndicatorSetters = GetStyleSetters(styles, "M3ScreenHeaderActivityIndicator");

            Assert.Contains("private readonly Border _busyIndicatorFrame;", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("_busyIndicatorFrame.SetDynamicResource(StyleProperty, \"M3ScreenHeaderBusyFrame\");", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("ActionContentOpacityAnimationName = \"M3ScreenHeaderActionContentOpacity\"", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("BusyFrameOpacityAnimationName = \"M3ScreenHeaderBusyFrameOpacity\"", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("SupportingTextOpacityAnimationName = \"M3ScreenHeaderSupportingTextOpacity\"", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("DetailTextOpacityAnimationName = \"M3ScreenHeaderDetailTextOpacity\"", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnActionContentVisibilityPropertyChanged", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnBusyPropertyChanged", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnSupportingTextVisibilityPropertyChanged", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("OnDetailTextVisibilityPropertyChanged", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialMotion.UpdateDouble(", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("MaterialResources.Get<int>(\"M3MotionStatusDuration\")", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteActionContentVisibility", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteBusyState", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteSupportingTextVisibility", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("CompleteDetailTextVisibility", screenHeaderView, StringComparison.Ordinal);
            Assert.Contains("<x:Int32 x:Key=\"M3MotionStatusDuration\">120</x:Int32>", interaction, StringComparison.Ordinal);
            Assert.DoesNotContain("_actionContentHost.IsVisible = hasActionContent", screenHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain("_busyIndicatorFrame.IsVisible = IsBusy;", screenHeaderView, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText)",
                screenHeaderView,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "_detailText.IsVisible = IsDetailTextVisible && !string.IsNullOrWhiteSpace(detailText)",
                screenHeaderView,
                StringComparison.Ordinal);
            Assert.Equal("{StaticResource TouchTarget}", busyFrameSetters["WidthRequest"]);
            Assert.Equal("{StaticResource TouchTarget}", busyFrameSetters["HeightRequest"]);
            Assert.Equal("{StaticResource M3Transparent}", busyFrameSetters["Stroke"]);
            Assert.Equal("{StaticResource M3StrokeNone}", busyFrameSetters["StrokeThickness"]);
            Assert.Equal(
                "{AppThemeBinding Light={StaticResource M3LightSurfaceContainerLow}, Dark={StaticResource M3DarkSurfaceContainerLow}}",
                busyFrameSetters["BackgroundColor"]);
            Assert.Equal("{StaticResource M3ScreenHeaderActivityIndicatorSize}", busyIndicatorSetters["WidthRequest"]);
            Assert.Equal("{StaticResource M3ScreenHeaderActivityIndicatorSize}", busyIndicatorSetters["HeightRequest"]);
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

            Assert.Contains("IsStatusVisible=\"{Binding IsNeutralStatusVisible}\"", notificationSettingsPage, StringComparison.Ordinal);
            Assert.DoesNotContain("IsVisible=\"{Binding IsNeutralStatusVisible}\"", notificationSettingsPage, StringComparison.Ordinal);
        }

        [Fact]
        public void Page_and_control_xaml_avoid_raw_material_layout_hotspots()
        {
            string[] disallowedPatterns =
            [
                "Style=\"{StaticResource M3",
                "<Grid",
                "<VerticalStackLayout",
                "<HorizontalStackLayout",
                "<ScrollView",
                "<CollectionView",
                "<RefreshView",
                "<Image",
                "<Label",
                "<Border",
                "<ProgressBar",
                "<toolkit:MediaElement",
                "<FlexLayout",
            ];

            string repositoryRoot = FindRepositoryRoot(StylesResourcePath);
            string mobileRoot = Path.Combine(repositoryRoot, "src", "Cotton.Mobile");
            string resourcesSegment = $"{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}";
            string binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
            string objSegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
            string[] xamlFiles = Directory.GetFiles(mobileRoot, "*.xaml", SearchOption.AllDirectories)
                .Where(path =>
                    !path.Contains(resourcesSegment, StringComparison.Ordinal)
                    && !path.Contains(binSegment, StringComparison.Ordinal)
                    && !path.Contains(objSegment, StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.NotEmpty(xamlFiles);

            foreach (string path in xamlFiles)
            {
                string relativePath = Path.GetRelativePath(repositoryRoot, path);
                string content = File.ReadAllText(path);

                foreach (string disallowedPattern in disallowedPatterns)
                {
                    Assert.False(
                        content.Contains(disallowedPattern, StringComparison.Ordinal),
                        $"{relativePath} contains {disallowedPattern}.");
                }
            }
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
