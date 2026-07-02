// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public abstract class TrashEntryCardViewBase : ContentView
    {
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(TrashEntryCardViewBase),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailProperty = BindableProperty.Create(
            nameof(Detail),
            typeof(string),
            typeof(TrashEntryCardViewBase),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(
            nameof(BadgeText),
            typeof(string),
            typeof(TrashEntryCardViewBase),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ThumbnailSourceProperty = BindableProperty.Create(
            nameof(ThumbnailSource),
            typeof(ImageSource),
            typeof(TrashEntryCardViewBase),
            default(ImageSource),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPreviewImageVisibleProperty = BindableProperty.Create(
            nameof(IsPreviewImageVisible),
            typeof(bool),
            typeof(TrashEntryCardViewBase),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsFolderThumbnailVisibleProperty = BindableProperty.Create(
            nameof(IsFolderThumbnailVisible),
            typeof(bool),
            typeof(TrashEntryCardViewBase),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLoadingProperty = BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(TrashEntryCardViewBase),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
            nameof(PlaceholderText),
            typeof(string),
            typeof(TrashEntryCardViewBase),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPlaceholderTextVisibleProperty = BindableProperty.Create(
            nameof(IsPlaceholderTextVisible),
            typeof(bool),
            typeof(TrashEntryCardViewBase),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(TrashEntryCardViewBase),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ToggleSelectionCommandProperty = BindableProperty.Create(
            nameof(ToggleSelectionCommand),
            typeof(ICommand),
            typeof(TrashEntryCardViewBase),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(TrashEntryCardViewBase),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsEntryActionsVisibleProperty = BindableProperty.Create(
            nameof(IsEntryActionsVisible),
            typeof(bool),
            typeof(TrashEntryCardViewBase),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DeleteForeverCommandProperty = BindableProperty.Create(
            nameof(DeleteForeverCommand),
            typeof(ICommand),
            typeof(TrashEntryCardViewBase),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty RestoreCommandProperty = BindableProperty.Create(
            nameof(RestoreCommand),
            typeof(ICommand),
            typeof(TrashEntryCardViewBase),
            propertyChanged: OnVisualPropertyChanged);

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

        public string BadgeText
        {
            get => (string)GetValue(BadgeTextProperty);
            set => SetValue(BadgeTextProperty, value);
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

        public ICommand? ToggleSelectionCommand
        {
            get => (ICommand?)GetValue(ToggleSelectionCommandProperty);
            set => SetValue(ToggleSelectionCommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public bool IsEntryActionsVisible
        {
            get => (bool)GetValue(IsEntryActionsVisibleProperty);
            set => SetValue(IsEntryActionsVisibleProperty, value);
        }

        public ICommand? DeleteForeverCommand
        {
            get => (ICommand?)GetValue(DeleteForeverCommandProperty);
            set => SetValue(DeleteForeverCommandProperty, value);
        }

        public ICommand? RestoreCommand
        {
            get => (ICommand?)GetValue(RestoreCommandProperty);
            set => SetValue(RestoreCommandProperty, value);
        }

        protected abstract void UpdateVisualState();

        protected void UpdateThumbnail(FileThumbnailView thumbnail)
        {
            thumbnail.ThumbnailSource = ThumbnailSource;
            thumbnail.IsPreviewImageVisible = IsPreviewImageVisible;
            thumbnail.IsFolderThumbnailVisible = IsFolderThumbnailVisible;
            thumbnail.IsLoading = IsLoading;
            thumbnail.PlaceholderText = PlaceholderText ?? string.Empty;
            thumbnail.IsPlaceholderTextVisible = IsPlaceholderTextVisible;
            thumbnail.IsSelected = IsSelected;
        }

        protected void UpdateTouchSurface(TouchSurfaceView touchSurface)
        {
            touchSurface.TapCommand = ToggleSelectionCommand;
            touchSurface.TapCommandParameter = CommandParameter;
        }

        protected void UpdateEntryActions(ActionClusterView actionCluster)
        {
            string title = string.IsNullOrWhiteSpace(Title) ? "item" : Title;

            actionCluster.IsVisible = IsEntryActionsVisible;
            actionCluster.PrimaryActionIconData = IconPathData.Delete;
            actionCluster.PrimaryActionCommand = DeleteForeverCommand;
            actionCluster.PrimaryActionCommandParameter = CommandParameter;
            actionCluster.PrimaryActionIconButtonStyleResourceKey = "M3DestructiveFileChromeIconButton";
            actionCluster.PrimaryActionSemanticDescription = $"Delete {title} forever";
            actionCluster.SecondaryActionIconData = IconPathData.Reset;
            actionCluster.SecondaryActionCommand = RestoreCommand;
            actionCluster.SecondaryActionCommandParameter = CommandParameter;
            actionCluster.SecondaryActionSemanticDescription = $"Restore {title}";
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TrashEntryCardViewBase view = (TrashEntryCardViewBase)bindable;
            view.UpdateVisualState();
        }
    }
}
