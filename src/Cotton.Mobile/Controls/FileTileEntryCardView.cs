// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Controls
{
    public class FileTileEntryCardView : Grid
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
            CottonFileTileLayoutPlanner.InitialMetrics.SlotWidth,
            propertyChanged: OnLayoutPropertyChanged);

        public static readonly BindableProperty TileHeightProperty = BindableProperty.Create(
            nameof(TileHeight),
            typeof(double),
            typeof(FileTileEntryCardView),
            CottonFileTileLayoutPlanner.InitialMetrics.TileHeight,
            propertyChanged: OnLayoutPropertyChanged);

        public static readonly BindableProperty PreviewHeightProperty = BindableProperty.Create(
            nameof(PreviewHeight),
            typeof(double),
            typeof(FileTileEntryCardView),
            CottonFileTileLayoutPlanner.InitialMetrics.PreviewHeight,
            propertyChanged: OnLayoutPropertyChanged);

        public static readonly BindableProperty FolderIconSizeProperty = BindableProperty.Create(
            nameof(FolderIconSize),
            typeof(double),
            typeof(FileTileEntryCardView),
            CottonFileTileLayoutPlanner.InitialMetrics.FolderIconSize,
            propertyChanged: OnLayoutPropertyChanged);

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

        private readonly Button _actionButton;
        private readonly Border _card;
        private readonly Grid _contentGrid;
        private readonly Label _detailLabel;
        private readonly Grid _metadataGrid;
        private readonly VerticalStackLayout _metadata;
        private readonly RowDefinition _previewRow;
        private readonly Border _thumbnail;
        private readonly Label _titleLabel;
        private readonly TouchSurfaceView _touchSurface;
        private ActivityIndicator? _thumbnailActivity;
        private IconView? _thumbnailIcon;
        private Image? _thumbnailImage;
        private Label? _thumbnailPlaceholder;
        private bool _isThumbnailSelectionIcon;
        private SelectionOverlayView? _selectionOverlay;
        private ChipView? _statusChip;
        private bool _isVisualStateUpdatePending;

        public FileTileEntryCardView()
        {
            _thumbnail = new Border();
            _thumbnail.SetDynamicResource(StyleProperty, "M3FilePreviewSurface");
            _titleLabel = new Label();
            _titleLabel.SetDynamicResource(StyleProperty, "M3CardSupportingStrongLine");
            _detailLabel = new Label();
            _detailLabel.SetDynamicResource(StyleProperty, "M3CardMetaLine");
            _metadataGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _detailLabel,
                },
            };
            _metadataGrid.SetDynamicResource(StyleProperty, "M3FileTileMetadataGrid");
            _metadata = new VerticalStackLayout
            {
                Children =
                {
                    _titleLabel,
                    _metadataGrid,
                },
            };
            _metadata.SetDynamicResource(StyleProperty, "M3FileTileTextStack");
            _touchSurface = new TouchSurfaceView();
            _actionButton = new Button
            {
                Text = "\u22ee",
            };
            ConfigureActionButton(_actionButton);

            _previewRow = new RowDefinition { Height = new GridLength(0) };

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
                    _thumbnail,
                    _metadata,
                    _touchSurface,
                    _actionButton,
                },
            };

            _card = new Border
            {
                Content = _contentGrid,
            };
            _card.SetDynamicResource(StyleProperty, "M3FileTileCard");
            Children.Add(_card);
            SetDynamicResource(StyleProperty, "M3FileTileSlotGrid");
            _contentGrid.SetDynamicResource(StyleProperty, "M3FileTileContentGrid");
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

        private static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileTileEntryCardView view = (FileTileEntryCardView)bindable;
            view.ApplyLayoutMetrics();
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
            ApplyLayoutMetrics();

            if (IsSelected || _selectionOverlay is not null)
            {
                EnsureSelectionOverlay().ApplySelectionState(IsSelected, animateSelection: false);
            }

            ApplyThumbnailState();
            ApplyMetadataState();

            _touchSurface.Command = BeginSelectionCommand;
            _touchSurface.CommandParameter = CommandParameter;
            _touchSurface.TapCommand = ActivateCommand;
            _touchSurface.TapCommandParameter = CommandParameter;

            _actionButton.Command = EntryActionsCommand;
            _actionButton.CommandParameter = CommandParameter;
            _actionButton.IsEnabled = IsActionEnabled;
            _actionButton.IsVisible = IsActionVisible;
            _actionButton.InputTransparent = !IsActionVisible || !IsActionEnabled;
            SemanticProperties.SetDescription(
                _actionButton,
                ActionSemanticDescription ?? string.Empty);
        }

        private void ApplyLayoutMetrics()
        {
            double slotWidth = SlotWidth > 0 ? SlotWidth : CottonFileTileLayoutPlanner.InitialMetrics.SlotWidth;
            double tileHeight = TileHeight > 0 ? TileHeight : CottonFileTileLayoutPlanner.InitialMetrics.TileHeight;
            double previewHeight = PreviewHeight > 0
                ? PreviewHeight
                : CottonFileTileLayoutPlanner.InitialMetrics.PreviewHeight;

            WidthRequest = slotWidth;
            _card.HeightRequest = tileHeight;
            _previewRow.Height = new GridLength(previewHeight);
            _thumbnail.HeightRequest = previewHeight;
        }

        private void ApplyThumbnailState()
        {
            if (IsSelected)
            {
                SetThumbnailContent(EnsureThumbnailIcon(showSelection: true));
                return;
            }

            if (IsPreviewImageVisible)
            {
                Image image = EnsureThumbnailImage();
                if (!Equals(image.Source, ThumbnailSource))
                {
                    image.Source = ThumbnailSource;
                }

                SetThumbnailContent(image);
                return;
            }

            if (IsFolderThumbnailVisible)
            {
                SetThumbnailContent(EnsureThumbnailIcon(showSelection: false));
                return;
            }

            if (IsLoading)
            {
                ActivityIndicator activity = EnsureThumbnailActivity();
                activity.IsRunning = true;
                SetThumbnailContent(activity);
                return;
            }

            Label placeholder = EnsureThumbnailPlaceholder();
            string placeholderText = IsPlaceholderTextVisible
                ? PlaceholderText ?? string.Empty
                : string.Empty;
            if (!string.Equals(placeholder.Text, placeholderText, StringComparison.Ordinal))
            {
                placeholder.Text = placeholderText;
            }

            SetThumbnailContent(placeholder);
        }

        private Image EnsureThumbnailImage()
        {
            return _thumbnailImage ??= new Image
            {
                Aspect = Aspect.AspectFill,
            };
        }

        private IconView EnsureThumbnailIcon(bool showSelection)
        {
            if (_thumbnailIcon is null)
            {
                _thumbnailIcon = new IconView();
                _isThumbnailSelectionIcon = !showSelection;
            }

            if (_isThumbnailSelectionIcon != showSelection)
            {
                _isThumbnailSelectionIcon = showSelection;
                _thumbnailIcon.IconData = showSelection ? IconPathData.Check : IconPathData.Folder;
                _thumbnailIcon.SetDynamicResource(
                    StyleProperty,
                    showSelection ? "M3FileSelectionCheckIcon" : "M3FolderThumbnailIcon");
            }

            if (!showSelection && FolderIconSize > 0)
            {
                _thumbnailIcon.IconSize = FolderIconSize;
            }

            return _thumbnailIcon;
        }

        private ActivityIndicator EnsureThumbnailActivity()
        {
            if (_thumbnailActivity is not null)
            {
                return _thumbnailActivity;
            }

            _thumbnailActivity = new ActivityIndicator();
            _thumbnailActivity.SetDynamicResource(StyleProperty, "M3ThumbnailActivityIndicator");
            return _thumbnailActivity;
        }

        private Label EnsureThumbnailPlaceholder()
        {
            if (_thumbnailPlaceholder is not null)
            {
                return _thumbnailPlaceholder;
            }

            _thumbnailPlaceholder = new Label();
            _thumbnailPlaceholder.SetDynamicResource(StyleProperty, "M3DynamicThumbnailPlaceholder");
            return _thumbnailPlaceholder;
        }

        private void SetThumbnailContent(View content)
        {
            if (ReferenceEquals(_thumbnail.Content, content))
            {
                return;
            }

            if (_thumbnail.Content is ActivityIndicator previousActivity)
            {
                previousActivity.IsRunning = false;
            }

            _thumbnail.Content = content;
        }

        private SelectionOverlayView EnsureSelectionOverlay()
        {
            if (_selectionOverlay is not null)
            {
                return _selectionOverlay;
            }

            _selectionOverlay = new SelectionOverlayView();
            Grid.SetRowSpan(_selectionOverlay, 2);
            _contentGrid.Children.Insert(0, _selectionOverlay);
            return _selectionOverlay;
        }

        private void ApplyMetadataState()
        {
            string title = Title ?? string.Empty;
            string detail = Detail ?? string.Empty;
            if (!string.Equals(_titleLabel.Text, title, StringComparison.Ordinal))
            {
                _titleLabel.Text = title;
            }

            if (!string.Equals(_detailLabel.Text, detail, StringComparison.Ordinal))
            {
                _detailLabel.Text = detail;
            }

            (string text, bool isVisible, string chipStyle, string textStyle) = CreateStatusChipState();
            if (isVisible)
            {
                ChipView statusChip = EnsureStatusChip();
                statusChip.ApplyChipState(
                    text,
                    chipStyle,
                    textStyle,
                    animateTextVisibility: false);
                statusChip.Opacity = MaterialMotion.Value("M3MotionVisibleOpacity");
                statusChip.IsVisible = true;
            }
            else if (_statusChip is not null)
            {
                _statusChip.IsVisible = false;
            }
        }

        private ChipView EnsureStatusChip()
        {
            if (_statusChip is not null)
            {
                return _statusChip;
            }

            _statusChip = new ChipView
            {
                IsVisible = false,
            };
            Grid.SetColumn(_statusChip, 1);
            _metadataGrid.Children.Add(_statusChip);
            return _statusChip;
        }

        private (string Text, bool IsVisible, string ChipStyle, string TextStyle) CreateStatusChipState()
        {
            string offlineAttentionStatus = OfflineAttentionStatus ?? string.Empty;
            if (IsOfflineAttentionVisible && !string.IsNullOrWhiteSpace(offlineAttentionStatus))
            {
                return (offlineAttentionStatus, true, "M3FileAttentionChip", "M3ErrorChipLabel");
            }

            string localCopyStatus = LocalCopyStatus ?? string.Empty;
            if (IsLocalCopyVisible && !string.IsNullOrWhiteSpace(localCopyStatus))
            {
                return (localCopyStatus, true, "M3LocalCopyChip", "M3LocalCopyChipLabel");
            }

            return (string.Empty, false, "M3NeutralChip", "M3ChipLabel");
        }

        private static void ConfigureActionButton(Button button)
        {
            double size = MaterialResources.Get<double>("M3FileActionSize");
            button.WidthRequest = size;
            button.HeightRequest = size;
            button.MinimumWidthRequest = size;
            button.MinimumHeightRequest = size;
            button.Padding = new Thickness(0);
            button.BorderWidth = MaterialResources.Get<double>("M3StrokeNone");
            button.FontSize = MaterialResources.Get<double>("M3FileActionIconSize");
            button.FontFamily = MaterialResources.Get<string>("M3FontFamilyMedium");
            button.FontAttributes = FontAttributes.None;
            button.HorizontalOptions = LayoutOptions.End;
            button.VerticalOptions = LayoutOptions.Start;
            button.Margin = MaterialResources.Get<Thickness>("M3FileTileActionMargin");
            button.BackgroundColor = MaterialResources.Get<Color>("M3Transparent");
            MaterialResources.SetThemeColor(
                button,
                Button.TextColorProperty,
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant");
        }
    }
}
