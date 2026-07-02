// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public class FileListEntryRowView : ContentView
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

        private readonly FileEntryActionButtonView _actionButton;
        private readonly Grid _grid;
        private readonly FileListMetadataView _metadata;
        private readonly SelectionOverlayView _selectionOverlay;
        private readonly FileThumbnailView _thumbnail;
        private readonly TouchSurfaceView _touchSurface;

        public FileListEntryRowView()
        {
            _selectionOverlay = new SelectionOverlayView
            {
                OverlayStyleResourceKey = "M3FileSelectionRowOverlay",
            };
            _thumbnail = new FileThumbnailView();
            _metadata = new FileListMetadataView();
            _touchSurface = new TouchSurfaceView();
            _actionButton = new FileEntryActionButtonView();

            Grid.SetColumnSpan(_selectionOverlay, 3);
            Grid.SetColumn(_metadata, 1);
            Grid.SetColumnSpan(_touchSurface, 2);
            Grid.SetColumn(_actionButton, 2);

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(MaterialResources.Get<double>("M3FileListThumbnailColumnWidth")) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength(MaterialResources.Get<double>("M3FileActionSize")) },
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

            Content = _grid;
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
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _grid.SetDynamicResource(StyleProperty, "M3FileListRowGrid");

            _selectionOverlay.IsSelected = IsSelected;

            _thumbnail.ThumbnailSource = ThumbnailSource;
            _thumbnail.IsPreviewImageVisible = IsPreviewImageVisible;
            _thumbnail.IsFolderThumbnailVisible = IsFolderThumbnailVisible;
            _thumbnail.IsLoading = IsLoading;
            _thumbnail.PlaceholderText = PlaceholderText ?? string.Empty;
            _thumbnail.IsPlaceholderTextVisible = IsPlaceholderTextVisible;
            _thumbnail.IsSelected = IsSelected;

            _metadata.Title = Title ?? string.Empty;
            _metadata.Detail = Detail ?? string.Empty;

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
