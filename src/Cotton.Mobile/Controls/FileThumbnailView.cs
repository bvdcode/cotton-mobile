// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class FileThumbnailView : ContentView
    {
        private const string DefaultBadgeLabelStyleResourceKey = "M3ChipLabel";
        private const string DefaultBadgeStyleResourceKey = "M3FileTileOverlayChip";
        private const string DefaultSelectionMarkStyleResourceKey = "M3FileListSelectionMark";
        private const string DefaultSurfaceStyleResourceKey = "M3FileListThumbnailSurface";

        public static readonly BindableProperty ThumbnailSourceProperty = BindableProperty.Create(
            nameof(ThumbnailSource),
            typeof(ImageSource),
            typeof(FileThumbnailView),
            default(ImageSource),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPreviewImageVisibleProperty = BindableProperty.Create(
            nameof(IsPreviewImageVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsFolderThumbnailVisibleProperty = BindableProperty.Create(
            nameof(IsFolderThumbnailVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
            nameof(PlaceholderText),
            typeof(string),
            typeof(FileThumbnailView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPlaceholderTextVisibleProperty = BindableProperty.Create(
            nameof(IsPlaceholderTextVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnVisualPropertyChanged);

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
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsBadgeVisibleProperty = BindableProperty.Create(
            nameof(IsBadgeVisible),
            typeof(bool),
            typeof(FileThumbnailView),
            false,
            propertyChanged: OnVisualPropertyChanged);

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

        private readonly Border _badge;
        private readonly Label _badgeLabel;
        private readonly IconView _folderIcon;
        private readonly Image _image;
        private readonly ActivityIndicator _loadingIndicator;
        private readonly Label _placeholder;
        private readonly Border _selectionMark;
        private readonly IconView _selectionMarkIcon;
        private readonly Border _surface;

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

            _loadingIndicator = new ActivityIndicator();

            _placeholder = new Label();

            _badgeLabel = new Label();

            _badge = new Border
            {
                Content = _badgeLabel,
            };

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
            UpdateVisualState();
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
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
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
            _badge.SetDynamicResource(StyleProperty, badgeStyleResourceKey);
            _badgeLabel.SetDynamicResource(StyleProperty, badgeLabelStyleResourceKey);

            _image.Source = ThumbnailSource;
            _image.IsVisible = IsPreviewImageVisible;
            _folderIcon.IsVisible = IsFolderThumbnailVisible;
            _loadingIndicator.IsRunning = IsLoading;
            _loadingIndicator.IsVisible = IsLoading;
            _placeholder.Text = PlaceholderText ?? string.Empty;
            _placeholder.IsVisible = IsPlaceholderTextVisible;
            _selectionMark.IsVisible = IsSelected;
            _badgeLabel.Text = BadgeText ?? string.Empty;
            _badge.IsVisible = IsBadgeVisible;

            if (FolderIconSize > 0)
            {
                _folderIcon.IconSize = FolderIconSize;
                return;
            }

            _folderIcon.ClearValue(IconView.IconSizeProperty);
        }
    }
}
