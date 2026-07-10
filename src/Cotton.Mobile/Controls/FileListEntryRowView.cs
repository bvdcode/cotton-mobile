// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class FileListEntryRowView : Grid
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FileListEntryRowView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailProperty = BindableProperty.Create(
            nameof(Detail),
            typeof(string),
            typeof(FileListEntryRowView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LocalCopyStatusProperty = BindableProperty.Create(
            nameof(LocalCopyStatus),
            typeof(string),
            typeof(FileListEntryRowView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLocalCopyVisibleProperty = BindableProperty.Create(
            nameof(IsLocalCopyVisible),
            typeof(bool),
            typeof(FileListEntryRowView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty OfflineAttentionStatusProperty = BindableProperty.Create(
            nameof(OfflineAttentionStatus),
            typeof(string),
            typeof(FileListEntryRowView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsOfflineAttentionVisibleProperty = BindableProperty.Create(
            nameof(IsOfflineAttentionVisible),
            typeof(bool),
            typeof(FileListEntryRowView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ThumbnailSourceProperty = BindableProperty.Create(
            nameof(ThumbnailSource),
            typeof(ImageSource),
            typeof(FileListEntryRowView),
            default(ImageSource),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPreviewImageVisibleProperty = BindableProperty.Create(
            nameof(IsPreviewImageVisible),
            typeof(bool),
            typeof(FileListEntryRowView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsFolderThumbnailVisibleProperty = BindableProperty.Create(
            nameof(IsFolderThumbnailVisible),
            typeof(bool),
            typeof(FileListEntryRowView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(FileListEntryRowView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
            nameof(PlaceholderText),
            typeof(string),
            typeof(FileListEntryRowView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPlaceholderTextVisibleProperty = BindableProperty.Create(
            nameof(IsPlaceholderTextVisible),
            typeof(bool),
            typeof(FileListEntryRowView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(FileListEntryRowView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BeginSelectionCommandProperty = BindableProperty.Create(
            nameof(BeginSelectionCommand),
            typeof(ICommand),
            typeof(FileListEntryRowView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActivateCommandProperty = BindableProperty.Create(
            nameof(ActivateCommand),
            typeof(ICommand),
            typeof(FileListEntryRowView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty EntryActionsCommandProperty = BindableProperty.Create(
            nameof(EntryActionsCommand),
            typeof(ICommand),
            typeof(FileListEntryRowView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(FileListEntryRowView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(FileListEntryRowView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(FileListEntryRowView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
            typeof(string),
            typeof(FileListEntryRowView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Button _actionButton;
        private readonly Label _detailLabel;
        private readonly Grid _metadata;
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

        public FileListEntryRowView()
        {
            _thumbnail = new Border();
            _thumbnail.SetDynamicResource(StyleProperty, "M3FileListThumbnailSurface");
            _titleLabel = new Label();
            _titleLabel.SetDynamicResource(StyleProperty, "M3CardTitle");
            _detailLabel = new Label();
            _detailLabel.SetDynamicResource(StyleProperty, "M3CardSupportingLine");
            Grid.SetRow(_detailLabel, 1);
            Grid.SetColumnSpan(_detailLabel, 2);
            _metadata = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _titleLabel,
                    _detailLabel,
                },
            };
            _metadata.SetDynamicResource(StyleProperty, "M3FileListMetadataGrid");
            _touchSurface = new TouchSurfaceView();
            _actionButton = new Button
            {
                Text = "\u22ee",
            };
            ConfigureActionButton(_actionButton);

            Grid.SetColumn(_metadata, 1);
            Grid.SetColumnSpan(_touchSurface, 2);
            Grid.SetColumn(_actionButton, 2);

            ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(MaterialResources.Get<double>("M3FileListThumbnailColumnWidth")),
                });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(MaterialResources.Get<double>("M3FileActionSize")),
                });
            Children.Add(_thumbnail);
            Children.Add(_metadata);
            Children.Add(_touchSurface);
            Children.Add(_actionButton);
            SetDynamicResource(StyleProperty, "M3FileListRowGrid");
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
            FileListEntryRowView view = (FileListEntryRowView)bindable;
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

            _selectionOverlay = new SelectionOverlayView
            {
                OverlayStyleResourceKey = "M3FileSelectionRowOverlay",
            };
            Grid.SetColumnSpan(_selectionOverlay, 3);
            Children.Insert(0, _selectionOverlay);
            return _selectionOverlay;
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
            button.VerticalOptions = LayoutOptions.Center;
            button.BackgroundColor = MaterialResources.Get<Color>("M3Transparent");
            MaterialResources.SetThemeColor(
                button,
                Button.TextColorProperty,
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant");
        }

        private void ApplyMetadataState()
        {
            (string trailingText, bool isTrailingTextVisible, string trailingChipStyle, string trailingTextStyle) =
                CreateStatusChipState();
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

            if (isTrailingTextVisible)
            {
                ChipView statusChip = EnsureStatusChip();
                statusChip.ApplyChipState(
                    trailingText,
                    trailingChipStyle,
                    trailingTextStyle,
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
            _metadata.Children.Add(_statusChip);
            return _statusChip;
        }

        private (string Text, bool IsVisible, string ChipStyle, string TextStyle) CreateStatusChipState()
        {
            string localCopyStatus = LocalCopyStatus ?? string.Empty;
            string offlineAttentionStatus = OfflineAttentionStatus ?? string.Empty;
            if (IsOfflineAttentionVisible && !string.IsNullOrWhiteSpace(offlineAttentionStatus))
            {
                return (offlineAttentionStatus, true, "M3FileAttentionChip", "M3ErrorChipLabel");
            }

            if (IsLocalCopyVisible && !string.IsNullOrWhiteSpace(localCopyStatus))
            {
                return (localCopyStatus, true, "M3LocalCopyChip", "M3LocalCopyChipLabel");
            }

            return (string.Empty, false, "M3NeutralChip", "M3ChipLabel");
        }
    }
}
