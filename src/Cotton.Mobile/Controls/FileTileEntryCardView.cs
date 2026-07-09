// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class FileTileEntryCardView : ContentView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FileTileEntryCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailProperty = BindableProperty.Create(
            nameof(Detail),
            typeof(string),
            typeof(FileTileEntryCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LocalCopyStatusProperty = BindableProperty.Create(
            nameof(LocalCopyStatus),
            typeof(string),
            typeof(FileTileEntryCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLocalCopyVisibleProperty = BindableProperty.Create(
            nameof(IsLocalCopyVisible),
            typeof(bool),
            typeof(FileTileEntryCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty OfflineAttentionStatusProperty = BindableProperty.Create(
            nameof(OfflineAttentionStatus),
            typeof(string),
            typeof(FileTileEntryCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsOfflineAttentionVisibleProperty = BindableProperty.Create(
            nameof(IsOfflineAttentionVisible),
            typeof(bool),
            typeof(FileTileEntryCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ThumbnailSourceProperty = BindableProperty.Create(
            nameof(ThumbnailSource),
            typeof(ImageSource),
            typeof(FileTileEntryCardView),
            default(ImageSource),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPreviewImageVisibleProperty = BindableProperty.Create(
            nameof(IsPreviewImageVisible),
            typeof(bool),
            typeof(FileTileEntryCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsFolderThumbnailVisibleProperty = BindableProperty.Create(
            nameof(IsFolderThumbnailVisible),
            typeof(bool),
            typeof(FileTileEntryCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(FileTileEntryCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
            nameof(PlaceholderText),
            typeof(string),
            typeof(FileTileEntryCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPlaceholderTextVisibleProperty = BindableProperty.Create(
            nameof(IsPlaceholderTextVisible),
            typeof(bool),
            typeof(FileTileEntryCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(FileTileEntryCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SlotWidthProperty = BindableProperty.Create(
            nameof(SlotWidth),
            typeof(double),
            typeof(FileTileEntryCardView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TileHeightProperty = BindableProperty.Create(
            nameof(TileHeight),
            typeof(double),
            typeof(FileTileEntryCardView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PreviewHeightProperty = BindableProperty.Create(
            nameof(PreviewHeight),
            typeof(double),
            typeof(FileTileEntryCardView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty FolderIconSizeProperty = BindableProperty.Create(
            nameof(FolderIconSize),
            typeof(double),
            typeof(FileTileEntryCardView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BeginSelectionCommandProperty = BindableProperty.Create(
            nameof(BeginSelectionCommand),
            typeof(ICommand),
            typeof(FileTileEntryCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActivateCommandProperty = BindableProperty.Create(
            nameof(ActivateCommand),
            typeof(ICommand),
            typeof(FileTileEntryCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty EntryActionsCommandProperty = BindableProperty.Create(
            nameof(EntryActionsCommand),
            typeof(ICommand),
            typeof(FileTileEntryCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(FileTileEntryCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(FileTileEntryCardView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(FileTileEntryCardView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
            typeof(string),
            typeof(FileTileEntryCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly FileEntryActionButtonView _actionButton;
        private readonly ContentCardView _card;
        private readonly Grid _contentGrid;
        private readonly FileTileMetadataView _metadata;
        private readonly RowDefinition _previewRow;
        private readonly SelectionOverlayView _selectionOverlay;
        private readonly Grid _slotGrid;
        private readonly FileThumbnailView _thumbnail;
        private readonly TouchSurfaceView _touchSurface;
        private bool _isVisualStateUpdatePending;

        public FileTileEntryCardView()
        {
            _selectionOverlay = new SelectionOverlayView();
            _thumbnail = new FileThumbnailView
            {
                SurfaceStyleResourceKey = "M3FilePreviewSurface",
                SelectionMarkStyleResourceKey = "M3FileTileSelectionMark",
            };
            _metadata = new FileTileMetadataView();
            _touchSurface = new TouchSurfaceView();
            _actionButton = new FileEntryActionButtonView
            {
                IconButtonStyleResourceKey = "M3FileTileActionIconButton",
            };

            _previewRow = new RowDefinition { Height = new GridLength(0) };

            Grid.SetRowSpan(_selectionOverlay, 2);
            Grid.SetRow(_metadata, 1);
            Grid.SetRowSpan(_touchSurface, 2);
            Grid.SetRow(_actionButton, 0);

            _contentGrid = new Grid
            {
                RowDefinitions =
                {
                    _previewRow,
                    new RowDefinition { Height = GridLength.Star },
                },
                Children =
                {
                    _selectionOverlay,
                    _thumbnail,
                    _metadata,
                    _touchSurface,
                    _actionButton,
                },
            };

            _card = new ContentCardView
            {
                CardStyleResourceKey = "M3FileTileCard",
                BodyContent = _contentGrid,
            };

            _slotGrid = new Grid
            {
                Children =
                {
                    _card,
                },
            };

            Content = _slotGrid;
            UpdateVisualState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Detail
        {
            get => (string)GetValue(DetailProperty);
            set => SetValue(DetailProperty, value);
        }

        public string LocalCopyStatus
        {
            get => (string)GetValue(LocalCopyStatusProperty);
            set => SetValue(LocalCopyStatusProperty, value);
        }

        public bool IsLocalCopyVisible
        {
            get => (bool)GetValue(IsLocalCopyVisibleProperty);
            set => SetValue(IsLocalCopyVisibleProperty, value);
        }

        public string OfflineAttentionStatus
        {
            get => (string)GetValue(OfflineAttentionStatusProperty);
            set => SetValue(OfflineAttentionStatusProperty, value);
        }

        public bool IsOfflineAttentionVisible
        {
            get => (bool)GetValue(IsOfflineAttentionVisibleProperty);
            set => SetValue(IsOfflineAttentionVisibleProperty, value);
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

        public double SlotWidth
        {
            get => (double)GetValue(SlotWidthProperty);
            set => SetValue(SlotWidthProperty, value);
        }

        public double TileHeight
        {
            get => (double)GetValue(TileHeightProperty);
            set => SetValue(TileHeightProperty, value);
        }

        public double PreviewHeight
        {
            get => (double)GetValue(PreviewHeightProperty);
            set => SetValue(PreviewHeightProperty, value);
        }

        public double FolderIconSize
        {
            get => (double)GetValue(FolderIconSizeProperty);
            set => SetValue(FolderIconSizeProperty, value);
        }

        public ICommand? BeginSelectionCommand
        {
            get => (ICommand?)GetValue(BeginSelectionCommandProperty);
            set => SetValue(BeginSelectionCommandProperty, value);
        }

        public ICommand? ActivateCommand
        {
            get => (ICommand?)GetValue(ActivateCommandProperty);
            set => SetValue(ActivateCommandProperty, value);
        }

        public ICommand? EntryActionsCommand
        {
            get => (ICommand?)GetValue(EntryActionsCommandProperty);
            set => SetValue(EntryActionsCommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileTileEntryCardView view = (FileTileEntryCardView)bindable;
            view.ScheduleVisualStateUpdate();
        }

        private void ScheduleVisualStateUpdate()
        {
            if (_isVisualStateUpdatePending)
            {
                return;
            }

            _isVisualStateUpdatePending = true;
            if (Dispatcher.Dispatch(ApplyPendingVisualStateUpdate))
            {
                return;
            }

            ApplyPendingVisualStateUpdate();
        }

        private void ApplyPendingVisualStateUpdate()
        {
            _isVisualStateUpdatePending = false;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _slotGrid.SetDynamicResource(StyleProperty, "M3FileTileSlotGrid");
            _slotGrid.WidthRequest = SlotWidth;
            _card.HeightRequest = TileHeight;
            _contentGrid.SetDynamicResource(StyleProperty, "M3FileTileContentGrid");
            _previewRow.Height = new GridLength(PreviewHeight);

            _selectionOverlay.IsSelected = IsSelected;

            _thumbnail.HeightRequest = PreviewHeight;
            _thumbnail.ThumbnailSource = ThumbnailSource;
            _thumbnail.IsPreviewImageVisible = IsPreviewImageVisible;
            _thumbnail.IsFolderThumbnailVisible = IsFolderThumbnailVisible;
            _thumbnail.FolderIconSize = FolderIconSize;
            _thumbnail.IsLoading = IsLoading;
            _thumbnail.PlaceholderText = PlaceholderText ?? string.Empty;
            _thumbnail.IsPlaceholderTextVisible = IsPlaceholderTextVisible;
            _thumbnail.IsSelected = IsSelected;

            _metadata.Title = Title ?? string.Empty;
            _metadata.Detail = Detail ?? string.Empty;
            _metadata.LocalCopyStatus = LocalCopyStatus ?? string.Empty;
            _metadata.IsLocalCopyVisible = IsLocalCopyVisible;
            _metadata.OfflineAttentionStatus = OfflineAttentionStatus ?? string.Empty;
            _metadata.IsOfflineAttentionVisible = IsOfflineAttentionVisible;

            _touchSurface.Command = BeginSelectionCommand;
            _touchSurface.CommandParameter = CommandParameter;
            _touchSurface.TapCommand = ActivateCommand;
            _touchSurface.TapCommandParameter = CommandParameter;

            _actionButton.Command = EntryActionsCommand;
            _actionButton.CommandParameter = CommandParameter;
            _actionButton.IsActionEnabled = IsActionEnabled;
            _actionButton.IsActionVisible = IsActionVisible;
            _actionButton.SemanticDescription = ActionSemanticDescription ?? string.Empty;
        }
    }
}
