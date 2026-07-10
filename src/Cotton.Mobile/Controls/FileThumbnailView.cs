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
        private string? _appliedBadgeLabelStyleResourceKey;
        private string? _appliedBadgeStyleResourceKey;
        private string? _appliedSelectionMarkStyleResourceKey;
        private string? _appliedSurfaceStyleResourceKey;
        private bool _hasAppliedBadgeVisibility;
        private bool _hasAppliedFolderIconVisibility;
        private bool _hasAppliedLoadingState;
        private bool _hasAppliedPlaceholderVisibility;
        private bool _hasAppliedPreviewImageVisibility;
        private bool _hasAppliedSelectionState;
        private bool _isCurrentBadgeVisible;
        private bool _isCurrentFolderThumbnailVisible;
        private bool _isCurrentLoading;
        private bool _isCurrentPlaceholderTextVisible;
        private bool _isCurrentPreviewImageVisible;
        private bool _isCurrentSelected;
        private string _currentBadgeText = string.Empty;
        private string _currentPlaceholderText = string.Empty;

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

            _folderIcon.SetDynamicResource(StyleProperty, "M3FolderThumbnailIcon");
            _loadingIndicator.SetDynamicResource(StyleProperty, "M3ThumbnailActivityIndicator");
            _placeholder.SetDynamicResource(StyleProperty, "M3DynamicThumbnailPlaceholder");
            _selectionMarkIcon.SetDynamicResource(StyleProperty, "M3FileSelectionCheckIcon");
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
            ApplyResolvedVisualState(
                ThumbnailSource,
                IsPreviewImageVisible,
                IsFolderThumbnailVisible,
                IsLoading,
                PlaceholderText ?? string.Empty,
                IsPlaceholderTextVisible,
                IsSelected,
                FolderIconSize,
                BadgeText ?? string.Empty,
                IsBadgeVisible,
                SurfaceStyleResourceKey,
                SelectionMarkStyleResourceKey,
                BadgeStyleResourceKey,
                BadgeLabelStyleResourceKey,
                animatePreviewImageVisibility,
                animateFolderIconVisibility,
                animatePlaceholderVisibility,
                animateLoading,
                animateBadgeVisibility,
                animateSelection);
        }

        internal void ApplyThumbnailState(
            ImageSource? thumbnailSource,
            bool isPreviewImageVisible,
            bool isFolderThumbnailVisible,
            bool isLoading,
            string placeholderText,
            bool isPlaceholderTextVisible,
            bool isSelected,
            double folderIconSize = 0d,
            string badgeText = "",
            bool isBadgeVisible = false,
            bool animateChanges = true)
        {
            ApplyResolvedVisualState(
                thumbnailSource,
                isPreviewImageVisible,
                isFolderThumbnailVisible,
                isLoading,
                placeholderText,
                isPlaceholderTextVisible,
                isSelected,
                folderIconSize,
                badgeText,
                isBadgeVisible,
                SurfaceStyleResourceKey,
                SelectionMarkStyleResourceKey,
                BadgeStyleResourceKey,
                BadgeLabelStyleResourceKey,
                animatePreviewImageVisibility: animateChanges,
                animateFolderIconVisibility: animateChanges,
                animatePlaceholderVisibility: animateChanges,
                animateLoading: animateChanges,
                animateBadgeVisibility: animateChanges,
                animateSelection: animateChanges);
        }

        private void ApplyResolvedVisualState(
            ImageSource? thumbnailSource,
            bool isPreviewImageVisible,
            bool isFolderThumbnailVisible,
            bool isLoading,
            string placeholderText,
            bool isPlaceholderTextVisible,
            bool isSelected,
            double folderIconSize,
            string badgeText,
            bool isBadgeVisible,
            string requestedSurfaceStyleResourceKey,
            string requestedSelectionMarkStyleResourceKey,
            string requestedBadgeStyleResourceKey,
            string requestedBadgeLabelStyleResourceKey,
            bool animatePreviewImageVisibility,
            bool animateFolderIconVisibility,
            bool animatePlaceholderVisibility,
            bool animateLoading,
            bool animateBadgeVisibility,
            bool animateSelection)
        {
            string surfaceStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedSurfaceStyleResourceKey,
                DefaultSurfaceStyleResourceKey);
            string selectionMarkStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedSelectionMarkStyleResourceKey,
                DefaultSelectionMarkStyleResourceKey);
            string badgeStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedBadgeStyleResourceKey,
                DefaultBadgeStyleResourceKey);
            string badgeLabelStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedBadgeLabelStyleResourceKey,
                DefaultBadgeLabelStyleResourceKey);
            string currentPlaceholderText = placeholderText ?? string.Empty;
            string currentBadgeText = badgeText ?? string.Empty;

            ApplyStyleIfChanged(_surface, surfaceStyleResourceKey, ref _appliedSurfaceStyleResourceKey);
            ApplyStyleIfChanged(
                _selectionMark,
                selectionMarkStyleResourceKey,
                ref _appliedSelectionMarkStyleResourceKey);
            if (!Equals(_image.Source, thumbnailSource))
            {
                _image.Source = thumbnailSource;
            }

            if (!_hasAppliedPreviewImageVisibility || _isCurrentPreviewImageVisible != isPreviewImageVisible)
            {
                _isCurrentPreviewImageVisible = isPreviewImageVisible;
                UpdatePreviewImageVisibility(isPreviewImageVisible, animatePreviewImageVisibility);
            }

            if (!_hasAppliedFolderIconVisibility || _isCurrentFolderThumbnailVisible != isFolderThumbnailVisible)
            {
                _isCurrentFolderThumbnailVisible = isFolderThumbnailVisible;
                UpdateFolderIconVisibility(isFolderThumbnailVisible, animateFolderIconVisibility);
            }

            if (!_hasAppliedLoadingState || _isCurrentLoading != isLoading)
            {
                _isCurrentLoading = isLoading;
                UpdateLoadingState(isLoading, animateLoading);
            }

            if (!string.Equals(_placeholder.Text, currentPlaceholderText, StringComparison.Ordinal))
            {
                _placeholder.Text = currentPlaceholderText;
            }

            if (!_hasAppliedPlaceholderVisibility
                || _isCurrentPlaceholderTextVisible != isPlaceholderTextVisible
                || !string.Equals(_currentPlaceholderText, currentPlaceholderText, StringComparison.Ordinal))
            {
                _currentPlaceholderText = currentPlaceholderText;
                _isCurrentPlaceholderTextVisible = isPlaceholderTextVisible;
                UpdatePlaceholderVisibility(
                    currentPlaceholderText,
                    isPlaceholderTextVisible,
                    animatePlaceholderVisibility);
            }

            _selectionMark.IsVisible = true;
            if (!_hasAppliedSelectionState || _isCurrentSelected != isSelected)
            {
                _isCurrentSelected = isSelected;
                UpdateSelectionState(isSelected, animateSelection);
            }

            bool badgeStyleChanged = !string.Equals(
                    _appliedBadgeStyleResourceKey,
                    badgeStyleResourceKey,
                    StringComparison.Ordinal)
                || !string.Equals(
                    _appliedBadgeLabelStyleResourceKey,
                    badgeLabelStyleResourceKey,
                    StringComparison.Ordinal);
            if (badgeStyleChanged
                || !string.Equals(_currentBadgeText, currentBadgeText, StringComparison.Ordinal)
                || _isCurrentBadgeVisible != isBadgeVisible)
            {
                _badge.ApplyChipState(
                    currentBadgeText,
                    badgeStyleResourceKey,
                    badgeLabelStyleResourceKey,
                    animateBadgeVisibility);
                _appliedBadgeStyleResourceKey = badgeStyleResourceKey;
                _appliedBadgeLabelStyleResourceKey = badgeLabelStyleResourceKey;
                _currentBadgeText = currentBadgeText;
                _isCurrentBadgeVisible = isBadgeVisible;
                UpdateBadgeVisibility(currentBadgeText, isBadgeVisible, animateBadgeVisibility);
            }

            if (folderIconSize > 0)
            {
                _folderIcon.IconSize = folderIconSize;
                return;
            }

            _folderIcon.ClearValue(IconView.IconSizeProperty);
        }

        private static void ApplyStyleIfChanged(
            Element target,
            string styleResourceKey,
            ref string? appliedStyleResourceKey)
        {
            if (string.Equals(appliedStyleResourceKey, styleResourceKey, StringComparison.Ordinal))
            {
                return;
            }

            target.SetDynamicResource(StyleProperty, styleResourceKey);
            appliedStyleResourceKey = styleResourceKey;
        }

        private void UpdatePreviewImageVisibility(
            bool isPreviewImageVisible,
            bool animatePreviewImageVisibility)
        {
            UpdateThumbnailLayerVisibility(
                _image,
                isPreviewImageVisible,
                animatePreviewImageVisibility,
                ref _hasAppliedPreviewImageVisibility,
                PreviewImageOpacityAnimationName,
                CompletePreviewImageVisibility);
        }

        private void UpdateFolderIconVisibility(
            bool isFolderThumbnailVisible,
            bool animateFolderIconVisibility)
        {
            UpdateThumbnailLayerVisibility(
                _folderIcon,
                isFolderThumbnailVisible,
                animateFolderIconVisibility,
                ref _hasAppliedFolderIconVisibility,
                FolderIconOpacityAnimationName,
                CompleteFolderIconVisibility);
        }

        private void UpdatePlaceholderVisibility(
            string placeholderText,
            bool isPlaceholderTextVisible,
            bool animatePlaceholderVisibility)
        {
            UpdateThumbnailLayerVisibility(
                _placeholder,
                IsPlaceholderActuallyVisible(placeholderText, isPlaceholderTextVisible),
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
            if (_isCurrentPreviewImageVisible)
            {
                _image.IsVisible = true;
                return;
            }

            _image.IsVisible = false;
        }

        private void CompleteFolderIconVisibility()
        {
            if (_isCurrentFolderThumbnailVisible)
            {
                _folderIcon.IsVisible = true;
                return;
            }

            _folderIcon.IsVisible = false;
        }

        private void CompletePlaceholderVisibility()
        {
            if (IsPlaceholderActuallyVisible(_currentPlaceholderText, _isCurrentPlaceholderTextVisible))
            {
                _placeholder.IsVisible = true;
                return;
            }

            _placeholder.IsVisible = false;
        }

        private static bool IsPlaceholderActuallyVisible(string placeholderText, bool isPlaceholderTextVisible)
        {
            return isPlaceholderTextVisible && !string.IsNullOrWhiteSpace(placeholderText);
        }

        private void UpdateBadgeVisibility(
            string badgeText,
            bool isBadgeVisible,
            bool animateBadgeVisibility)
        {
            bool isBadgeActuallyVisible = IsBadgeActuallyVisible(badgeText, isBadgeVisible);
            bool shouldAnimate = animateBadgeVisibility && _hasAppliedBadgeVisibility;
            double targetOpacity = isBadgeActuallyVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isBadgeActuallyVisible)
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
            if (IsBadgeActuallyVisible(_currentBadgeText, _isCurrentBadgeVisible))
            {
                _badge.IsVisible = true;
                return;
            }

            _badge.IsVisible = false;
        }

        private static bool IsBadgeActuallyVisible(string badgeText, bool isBadgeVisible)
        {
            return isBadgeVisible && !string.IsNullOrWhiteSpace(badgeText);
        }

        private void UpdateLoadingState(bool isLoading, bool animateLoading)
        {
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
            if (_isCurrentLoading)
            {
                _loadingIndicator.IsVisible = true;
                _loadingIndicator.IsRunning = true;
                return;
            }

            _loadingIndicator.IsRunning = false;
            _loadingIndicator.IsVisible = false;
        }

        private void UpdateSelectionState(bool isSelected, bool animateSelection)
        {
            double targetOpacity = isSelected
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            double targetScale = isSelected
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
