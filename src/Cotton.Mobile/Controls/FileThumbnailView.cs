// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;

namespace Cotton.Mobile.Controls
{
    public class FileThumbnailView : ContentView
    {
        private const string DefaultBadgeLabelStyleResourceKey = "M3ChipLabel";
        private const string DefaultBadgeStyleResourceKey = "M3FileTileOverlayChip";
        private const string DefaultSelectionMarkStyleResourceKey = "M3FileListSelectionMark";
        private const string DefaultSurfaceStyleResourceKey = "M3FileListThumbnailSurface";
        private const string BadgeOpacityAnimationName = "M3FileThumbnailBadgeOpacity";
        private const string FolderIconOpacityAnimationName = "M3FileThumbnailFolderOpacity";
        private const string LoadingIndicatorOpacityAnimationName = "M3FileThumbnailLoadingOpacity";
        private const string PlaceholderOpacityAnimationName = "M3FileThumbnailPlaceholderOpacity";
        private const string PreviewImageOpacityAnimationName = "M3FileThumbnailPreviewOpacity";
        private const string SelectionMarkOpacityAnimationName = "M3FileSelectionMarkOpacity";
        private const string SelectionMarkScaleAnimationName = "M3FileSelectionMarkScale";

        public static readonly BindableProperty ThumbnailSourceProperty = BindableProperty.Create(
            nameof(ThumbnailSource),
            typeof(ImageSource),
            typeof(FileThumbnailView),
            default(ImageSource),
            propertyChanged: OnPreviewImageVisibilityPropertyChanged);

        public static readonly BindableProperty IsPreviewImageVisibleProperty = BindableProperty.Create(
            nameof(IsPreviewImageVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnPreviewImageVisibilityPropertyChanged);

        public static readonly BindableProperty IsFolderThumbnailVisibleProperty = BindableProperty.Create(
            nameof(IsFolderThumbnailVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnFolderIconVisibilityPropertyChanged);

        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnLoadingPropertyChanged);

        public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
            nameof(PlaceholderText),
            typeof(string),
            typeof(FileThumbnailView),
            string.Empty,
            propertyChanged: OnPlaceholderVisibilityPropertyChanged);

        public static readonly BindableProperty IsPlaceholderTextVisibleProperty = BindableProperty.Create(
            nameof(IsPlaceholderTextVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnPlaceholderVisibilityPropertyChanged);

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnSelectedPropertyChanged);

        public static readonly BindableProperty FolderIconSizeProperty = BindableProperty.Create(
            nameof(FolderIconSize),
            typeof(double),
            typeof(FileThumbnailView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(
            nameof(BadgeText),
            typeof(string),
            typeof(FileThumbnailView),
            string.Empty,
            propertyChanged: OnBadgeVisibilityPropertyChanged);

        public static readonly BindableProperty IsBadgeVisibleProperty = BindableProperty.Create(
            nameof(IsBadgeVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnBadgeVisibilityPropertyChanged);

        public static readonly BindableProperty SurfaceStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SurfaceStyleResourceKey),
            typeof(string),
            typeof(FileThumbnailView),
            DefaultSurfaceStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SelectionMarkStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SelectionMarkStyleResourceKey),
            typeof(string),
            typeof(FileThumbnailView),
            DefaultSelectionMarkStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BadgeStyleResourceKeyProperty = BindableProperty.Create(
            nameof(BadgeStyleResourceKey),
            typeof(string),
            typeof(FileThumbnailView),
            DefaultBadgeStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BadgeLabelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(BadgeLabelStyleResourceKey),
            typeof(string),
            typeof(FileThumbnailView),
            DefaultBadgeLabelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ChipView _badge;
        private readonly IconView _folderIcon;
        private readonly Image _image;
        private readonly ActivityIndicator _loadingIndicator;
        private readonly Label _placeholder;
        private readonly Border _selectionMark;
        private readonly IconView _selectionMarkIcon;
        private readonly Border _surface;
        private bool _hasAppliedBadgeVisibility;
        private bool _hasAppliedFolderIconVisibility;
        private bool _hasAppliedLoadingState;
        private bool _hasAppliedPlaceholderVisibility;
        private bool _hasAppliedPreviewImageVisibility;
        private bool _hasAppliedSelectionState;

        public FileThumbnailView()
        {
            InputTransparent = true;

            _image = new Image
            {
                Aspect = Aspect.AspectFill,
            };

            _folderIcon = new IconView
            {
                IconData = IconPathData.Folder,
            };

            _loadingIndicator = new ActivityIndicator
            {
                IsVisible = false,
                Opacity = MaterialMotion.Value("M3MotionHiddenOpacity"),
            };

            _placeholder = new Label();

            _badge = new ChipView();

            Grid surfaceContent = new()
            {
                Children =
                {
                    _image,
                    _folderIcon,
                    _loadingIndicator,
                    _placeholder,
                    _badge,
                },
            };

            _surface = new Border
            {
                Content = surfaceContent,
            };

            _selectionMarkIcon = new IconView
            {
                IconData = IconPathData.Check,
            };

            _selectionMark = new Border
            {
                Content = _selectionMarkIcon,
            };

            Grid root = new()
            {
                Children =
                {
                    _surface,
                    _selectionMark,
                },
            };

            Content = root;
            UpdateVisualState(animateSelection: false);
        }

        public ImageSource? ThumbnailSource
        {
            get => (ImageSource?)GetValue(ThumbnailSourceProperty);
            set => SetValue(ThumbnailSourceProperty, value);
        }

        public bool IsPreviewImageVisible
        {
            get => (bool)GetValue(IsPreviewImageVisibleProperty);
            set => SetValue(IsPreviewImageVisibleProperty, value);
        }

        public bool IsFolderThumbnailVisible
        {
            get => (bool)GetValue(IsFolderThumbnailVisibleProperty);
            set => SetValue(IsFolderThumbnailVisibleProperty, value);
        }

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public bool IsPlaceholderTextVisible
        {
            get => (bool)GetValue(IsPlaceholderTextVisibleProperty);
            set => SetValue(IsPlaceholderTextVisibleProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public double FolderIconSize
        {
            get => (double)GetValue(FolderIconSizeProperty);
            set => SetValue(FolderIconSizeProperty, value);
        }

        public string BadgeText
        {
            get => (string)GetValue(BadgeTextProperty);
            set => SetValue(BadgeTextProperty, value);
        }

        public bool IsBadgeVisible
        {
            get => (bool)GetValue(IsBadgeVisibleProperty);
            set => SetValue(IsBadgeVisibleProperty, value);
        }

        public string SurfaceStyleResourceKey
        {
            get => (string)GetValue(SurfaceStyleResourceKeyProperty);
            set => SetValue(SurfaceStyleResourceKeyProperty, value);
        }

        public string SelectionMarkStyleResourceKey
        {
            get => (string)GetValue(SelectionMarkStyleResourceKeyProperty);
            set => SetValue(SelectionMarkStyleResourceKeyProperty, value);
        }

        public string BadgeStyleResourceKey
        {
            get => (string)GetValue(BadgeStyleResourceKeyProperty);
            set => SetValue(BadgeStyleResourceKeyProperty, value);
        }

        public string BadgeLabelStyleResourceKey
        {
            get => (string)GetValue(BadgeLabelStyleResourceKeyProperty);
            set => SetValue(BadgeLabelStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileThumbnailView view = (FileThumbnailView)bindable;
            view.UpdateVisualState(
                animatePreviewImageVisibility: false,
                animateFolderIconVisibility: false,
                animatePlaceholderVisibility: false,
                animateLoading: false,
                animateBadgeVisibility: false,
                animateSelection: false);
        }

        private static void OnPreviewImageVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            FileThumbnailView view = (FileThumbnailView)bindable;
            view.UpdateVisualState(
                animatePreviewImageVisibility: true,
                animateFolderIconVisibility: false,
                animatePlaceholderVisibility: false,
                animateLoading: false,
                animateBadgeVisibility: false,
                animateSelection: false);
        }

        private static void OnFolderIconVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            FileThumbnailView view = (FileThumbnailView)bindable;
            view.UpdateVisualState(
                animatePreviewImageVisibility: false,
                animateFolderIconVisibility: true,
                animatePlaceholderVisibility: false,
                animateLoading: false,
                animateBadgeVisibility: false,
                animateSelection: false);
        }

        private static void OnPlaceholderVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            FileThumbnailView view = (FileThumbnailView)bindable;
            view.UpdateVisualState(
                animatePreviewImageVisibility: false,
                animateFolderIconVisibility: false,
                animatePlaceholderVisibility: true,
                animateLoading: false,
                animateBadgeVisibility: false,
                animateSelection: false);
        }

        private static void OnSelectedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileThumbnailView view = (FileThumbnailView)bindable;
            view.UpdateVisualState(
                animatePreviewImageVisibility: false,
                animateFolderIconVisibility: false,
                animatePlaceholderVisibility: false,
                animateLoading: false,
                animateBadgeVisibility: false,
                animateSelection: true);
        }

        private static void OnLoadingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileThumbnailView view = (FileThumbnailView)bindable;
            view.UpdateVisualState(
                animatePreviewImageVisibility: false,
                animateFolderIconVisibility: false,
                animatePlaceholderVisibility: false,
                animateLoading: true,
                animateBadgeVisibility: false,
                animateSelection: false);
        }

        private static void OnBadgeVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileThumbnailView view = (FileThumbnailView)bindable;
            view.UpdateVisualState(
                animatePreviewImageVisibility: false,
                animateFolderIconVisibility: false,
                animatePlaceholderVisibility: false,
                animateLoading: false,
                animateBadgeVisibility: true,
                animateSelection: false);
        }

        private void UpdateVisualState(bool animateSelection)
        {
            UpdateVisualState(
                animatePreviewImageVisibility: false,
                animateFolderIconVisibility: false,
                animatePlaceholderVisibility: false,
                animateLoading: false,
                animateBadgeVisibility: false,
                animateSelection);
        }

        private void UpdateVisualState(
            bool animatePreviewImageVisibility,
            bool animateFolderIconVisibility,
            bool animatePlaceholderVisibility,
            bool animateLoading,
            bool animateBadgeVisibility,
            bool animateSelection)
        {
            string surfaceStyleResourceKey = string.IsNullOrWhiteSpace(SurfaceStyleResourceKey)
                ? DefaultSurfaceStyleResourceKey
                : SurfaceStyleResourceKey;
            string selectionMarkStyleResourceKey = string.IsNullOrWhiteSpace(SelectionMarkStyleResourceKey)
                ? DefaultSelectionMarkStyleResourceKey
                : SelectionMarkStyleResourceKey;
            string badgeStyleResourceKey = string.IsNullOrWhiteSpace(BadgeStyleResourceKey)
                ? DefaultBadgeStyleResourceKey
                : BadgeStyleResourceKey;
            string badgeLabelStyleResourceKey = string.IsNullOrWhiteSpace(BadgeLabelStyleResourceKey)
                ? DefaultBadgeLabelStyleResourceKey
                : BadgeLabelStyleResourceKey;

            _surface.SetDynamicResource(StyleProperty, surfaceStyleResourceKey);
            _folderIcon.SetDynamicResource(StyleProperty, "M3FolderThumbnailIcon");
            _loadingIndicator.SetDynamicResource(StyleProperty, "M3ThumbnailActivityIndicator");
            _placeholder.SetDynamicResource(StyleProperty, "M3DynamicThumbnailPlaceholder");
            _selectionMark.SetDynamicResource(StyleProperty, selectionMarkStyleResourceKey);
            _selectionMarkIcon.SetDynamicResource(StyleProperty, "M3FileSelectionCheckIcon");
            _image.Source = ThumbnailSource;
            UpdatePreviewImageVisibility(animatePreviewImageVisibility);
            UpdateFolderIconVisibility(animateFolderIconVisibility);
            UpdateLoadingState(animateLoading);
            _placeholder.Text = PlaceholderText ?? string.Empty;
            UpdatePlaceholderVisibility(animatePlaceholderVisibility);
            _selectionMark.IsVisible = true;
            UpdateSelectionState(animateSelection);
            _badge.Text = BadgeText ?? string.Empty;
            _badge.ChipStyleResourceKey = badgeStyleResourceKey;
            _badge.LabelStyleResourceKey = badgeLabelStyleResourceKey;
            UpdateBadgeVisibility(animateBadgeVisibility);

            if (FolderIconSize > 0)
            {
                _folderIcon.IconSize = FolderIconSize;
                return;
            }

            _folderIcon.ClearValue(IconView.IconSizeProperty);
        }

        private void UpdatePreviewImageVisibility(bool animatePreviewImageVisibility)
        {
            UpdateThumbnailLayerVisibility(
                _image,
                IsPreviewImageVisible,
                animatePreviewImageVisibility,
                ref _hasAppliedPreviewImageVisibility,
                PreviewImageOpacityAnimationName,
                CompletePreviewImageVisibility);
        }

        private void UpdateFolderIconVisibility(bool animateFolderIconVisibility)
        {
            UpdateThumbnailLayerVisibility(
                _folderIcon,
                IsFolderThumbnailVisible,
                animateFolderIconVisibility,
                ref _hasAppliedFolderIconVisibility,
                FolderIconOpacityAnimationName,
                CompleteFolderIconVisibility);
        }

        private void UpdatePlaceholderVisibility(bool animatePlaceholderVisibility)
        {
            UpdateThumbnailLayerVisibility(
                _placeholder,
                IsPlaceholderActuallyVisible(),
                animatePlaceholderVisibility,
                ref _hasAppliedPlaceholderVisibility,
                PlaceholderOpacityAnimationName,
                CompletePlaceholderVisibility);
        }

        private void UpdateThumbnailLayerVisibility(
            VisualElement layer,
            bool isLayerVisible,
            bool animateLayerVisibility,
            ref bool hasAppliedLayerVisibility,
            string animationName,
            Action completeVisibility)
        {
            bool shouldAnimate = animateLayerVisibility && hasAppliedLayerVisibility;
            double targetOpacity = isLayerVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isLayerVisible)
            {
                layer.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                layer,
                layer.Opacity,
                targetOpacity,
                duration,
                animationName,
                shouldAnimate,
                opacity => layer.Opacity = opacity,
                completeVisibility);
            hasAppliedLayerVisibility = true;
        }

        private void CompletePreviewImageVisibility()
        {
            if (IsPreviewImageVisible)
            {
                _image.IsVisible = true;
                return;
            }

            _image.IsVisible = false;
        }

        private void CompleteFolderIconVisibility()
        {
            if (IsFolderThumbnailVisible)
            {
                _folderIcon.IsVisible = true;
                return;
            }

            _folderIcon.IsVisible = false;
        }

        private void CompletePlaceholderVisibility()
        {
            if (IsPlaceholderActuallyVisible())
            {
                _placeholder.IsVisible = true;
                return;
            }

            _placeholder.IsVisible = false;
        }

        private bool IsPlaceholderActuallyVisible()
        {
            return IsPlaceholderTextVisible && !string.IsNullOrWhiteSpace(PlaceholderText);
        }

        private void UpdateBadgeVisibility(bool animateBadgeVisibility)
        {
            bool isBadgeVisible = IsBadgeActuallyVisible();
            bool shouldAnimate = animateBadgeVisibility && _hasAppliedBadgeVisibility;
            double targetOpacity = isBadgeVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isBadgeVisible)
            {
                _badge.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _badge,
                _badge.Opacity,
                targetOpacity,
                duration,
                BadgeOpacityAnimationName,
                shouldAnimate,
                opacity => _badge.Opacity = opacity,
                CompleteBadgeVisibility);
            _hasAppliedBadgeVisibility = true;
        }

        private void CompleteBadgeVisibility()
        {
            if (IsBadgeActuallyVisible())
            {
                _badge.IsVisible = true;
                return;
            }

            _badge.IsVisible = false;
        }

        private bool IsBadgeActuallyVisible()
        {
            return IsBadgeVisible && !string.IsNullOrWhiteSpace(BadgeText);
        }

        private void UpdateLoadingState(bool animateLoading)
        {
            bool isLoading = IsLoading;
            bool shouldAnimate = animateLoading && _hasAppliedLoadingState;
            double targetOpacity = isLoading
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isLoading)
            {
                _loadingIndicator.IsVisible = true;
                _loadingIndicator.IsRunning = true;
            }

            MaterialMotion.UpdateDouble(
                _loadingIndicator,
                _loadingIndicator.Opacity,
                targetOpacity,
                duration,
                LoadingIndicatorOpacityAnimationName,
                shouldAnimate,
                opacity => _loadingIndicator.Opacity = opacity,
                CompleteLoadingState);
            _hasAppliedLoadingState = true;
        }

        private void CompleteLoadingState()
        {
            if (IsLoading)
            {
                _loadingIndicator.IsVisible = true;
                _loadingIndicator.IsRunning = true;
                return;
            }

            _loadingIndicator.IsRunning = false;
            _loadingIndicator.IsVisible = false;
        }

        private void UpdateSelectionState(bool animateSelection)
        {
            double targetOpacity = IsSelected
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            double targetScale = IsSelected
                ? MaterialMotion.Value("M3InteractionRestScale")
                : MaterialMotion.Value("M3MotionSelectionHiddenScale");
            bool shouldAnimate = animateSelection && _hasAppliedSelectionState;
            int duration = MaterialResources.Get<int>("M3MotionSelectionDuration");

            MaterialMotion.UpdateDouble(
                _selectionMark,
                _selectionMark.Opacity,
                targetOpacity,
                duration,
                SelectionMarkOpacityAnimationName,
                shouldAnimate,
                opacity => _selectionMark.Opacity = opacity);
            MaterialMotion.UpdateDouble(
                _selectionMark,
                _selectionMark.Scale,
                targetScale,
                duration,
                SelectionMarkScaleAnimationName,
                shouldAnimate,
                scale => _selectionMark.Scale = scale);
            _hasAppliedSelectionState = true;
        }
    }
}
