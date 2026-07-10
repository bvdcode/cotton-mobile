// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class FileBrowserNavigationBarView : ContentView
    {
        private const string DefaultSelectedItemStyleResourceKey = "M3NavigationBarItemSelected";
        private const string DefaultUnselectedItemStyleResourceKey = "M3NavigationBarItemUnselected";
        private const string DefaultSyncText = "Sync";
        private const string DefaultMoreText = "More";
        private const string NavigationOpacityAnimationName = "M3FileBrowserNavigationOpacity";

        public static readonly BindableProperty FilesTextProperty = BindableProperty.Create(
            nameof(FilesText),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty FilesSemanticDescriptionProperty = BindableProperty.Create(
            nameof(FilesSemanticDescription),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SyncTextProperty = BindableProperty.Create(
            nameof(SyncText),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            DefaultSyncText,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SyncCommandProperty = BindableProperty.Create(
            nameof(SyncCommand),
            typeof(ICommand),
            typeof(FileBrowserNavigationBarView),
            default(ICommand),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSyncEnabledProperty = BindableProperty.Create(
            nameof(IsSyncEnabled),
            typeof(bool),
            typeof(FileBrowserNavigationBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SyncSemanticDescriptionProperty = BindableProperty.Create(
            nameof(SyncSemanticDescription),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BackupTextProperty = BindableProperty.Create(
            nameof(BackupText),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BackupCommandProperty = BindableProperty.Create(
            nameof(BackupCommand),
            typeof(ICommand),
            typeof(FileBrowserNavigationBarView),
            default(ICommand),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsBackupEnabledProperty = BindableProperty.Create(
            nameof(IsBackupEnabled),
            typeof(bool),
            typeof(FileBrowserNavigationBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BackupSemanticDescriptionProperty = BindableProperty.Create(
            nameof(BackupSemanticDescription),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MoreTextProperty = BindableProperty.Create(
            nameof(MoreText),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            DefaultMoreText,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MoreCommandProperty = BindableProperty.Create(
            nameof(MoreCommand),
            typeof(ICommand),
            typeof(FileBrowserNavigationBarView),
            default(ICommand),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsMoreEnabledProperty = BindableProperty.Create(
            nameof(IsMoreEnabled),
            typeof(bool),
            typeof(FileBrowserNavigationBarView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MoreSemanticDescriptionProperty = BindableProperty.Create(
            nameof(MoreSemanticDescription),
            typeof(string),
            typeof(FileBrowserNavigationBarView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsNavigationVisibleProperty = BindableProperty.Create(
            nameof(IsNavigationVisible),
            typeof(bool),
            typeof(FileBrowserNavigationBarView),
            true,
            propertyChanged: OnNavigationVisiblePropertyChanged);

        private readonly NavigationBarItem _backupItem;
        private readonly NavigationBarItem _filesItem;
        private readonly NavigationBarItem _moreItem;
        private readonly NavigationBarView _navigationBar;
        private readonly NavigationBarItem _syncItem;
        private bool _hasAppliedNavigationVisibility;

        public FileBrowserNavigationBarView()
        {
            _filesItem = CreateItem(IconPathData.Folder, DefaultSelectedItemStyleResourceKey, 0);
            _syncItem = CreateItem(IconPathData.Transfer, DefaultUnselectedItemStyleResourceKey, 1);
            _backupItem = CreateItem(IconPathData.Backup, DefaultUnselectedItemStyleResourceKey, 2);
            _moreItem = CreateItem(IconPathData.MoreVertical, DefaultUnselectedItemStyleResourceKey, 3);

            _navigationBar = new NavigationBarView
            {
                Items =
                {
                    _filesItem,
                    _syncItem,
                    _backupItem,
                    _moreItem,
                },
            };

            Content = _navigationBar;
            UpdateVisualState();
            UpdateNavigationVisibility(animateNavigationVisibility: false);
        }

        public string FilesText
        {
            get => (string)GetValue(FilesTextProperty);
            set => SetValue(FilesTextProperty, value);
        }

        public string FilesSemanticDescription
        {
            get => (string)GetValue(FilesSemanticDescriptionProperty);
            set => SetValue(FilesSemanticDescriptionProperty, value);
        }

        public string SyncText
        {
            get => (string)GetValue(SyncTextProperty);
            set => SetValue(SyncTextProperty, value);
        }

        public ICommand? SyncCommand
        {
            get => (ICommand?)GetValue(SyncCommandProperty);
            set => SetValue(SyncCommandProperty, value);
        }

        public bool IsSyncEnabled
        {
            get => (bool)GetValue(IsSyncEnabledProperty);
            set => SetValue(IsSyncEnabledProperty, value);
        }

        public string SyncSemanticDescription
        {
            get => (string)GetValue(SyncSemanticDescriptionProperty);
            set => SetValue(SyncSemanticDescriptionProperty, value);
        }

        public string BackupText
        {
            get => (string)GetValue(BackupTextProperty);
            set => SetValue(BackupTextProperty, value);
        }

        public ICommand? BackupCommand
        {
            get => (ICommand?)GetValue(BackupCommandProperty);
            set => SetValue(BackupCommandProperty, value);
        }

        public bool IsBackupEnabled
        {
            get => (bool)GetValue(IsBackupEnabledProperty);
            set => SetValue(IsBackupEnabledProperty, value);
        }

        public string BackupSemanticDescription
        {
            get => (string)GetValue(BackupSemanticDescriptionProperty);
            set => SetValue(BackupSemanticDescriptionProperty, value);
        }

        public string MoreText
        {
            get => (string)GetValue(MoreTextProperty);
            set => SetValue(MoreTextProperty, value);
        }

        public ICommand? MoreCommand
        {
            get => (ICommand?)GetValue(MoreCommandProperty);
            set => SetValue(MoreCommandProperty, value);
        }

        public bool IsMoreEnabled
        {
            get => (bool)GetValue(IsMoreEnabledProperty);
            set => SetValue(IsMoreEnabledProperty, value);
        }

        public string MoreSemanticDescription
        {
            get => (string)GetValue(MoreSemanticDescriptionProperty);
            set => SetValue(MoreSemanticDescriptionProperty, value);
        }

        public bool IsNavigationVisible
        {
            get => (bool)GetValue(IsNavigationVisibleProperty);
            set => SetValue(IsNavigationVisibleProperty, value);
        }

        private static NavigationBarItem CreateItem(Geometry iconData, string styleResourceKey, int column)
        {
            NavigationBarItem item = new()
            {
                IconData = iconData,
            };
            item.SetDynamicResource(StyleProperty, styleResourceKey);
            Grid.SetColumn(item, column);

            return item;
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileBrowserNavigationBarView view = (FileBrowserNavigationBarView)bindable;
            view.UpdateVisualState();
        }

        private static void OnNavigationVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileBrowserNavigationBarView view = (FileBrowserNavigationBarView)bindable;
            view.UpdateNavigationVisibility(animateNavigationVisibility: true);
        }

        private void UpdateVisualState()
        {
            ApplyItem(_filesItem, FilesText, null, true, FilesSemanticDescription);
            ApplyItem(_syncItem, ResolveText(SyncText, DefaultSyncText), SyncCommand, IsSyncEnabled, SyncSemanticDescription);
            ApplyItem(_backupItem, BackupText, BackupCommand, IsBackupEnabled, BackupSemanticDescription);
            ApplyItem(_moreItem, ResolveText(MoreText, DefaultMoreText), MoreCommand, IsMoreEnabled, MoreSemanticDescription);
        }

        private void UpdateNavigationVisibility(bool animateNavigationVisibility)
        {
            bool isNavigationVisible = IsNavigationVisible;
            bool shouldAnimate = animateNavigationVisibility && _hasAppliedNavigationVisibility;
            double targetOpacity = isNavigationVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isNavigationVisible)
            {
                IsVisible = true;
            }

            UpdateInputTransparency();
            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                NavigationOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteNavigationVisibility);
            _hasAppliedNavigationVisibility = true;
        }

        private static void ApplyItem(
            NavigationBarItem item,
            string? text,
            ICommand? command,
            bool isEnabled,
            string? semanticDescription)
        {
            item.Text = text ?? string.Empty;
            item.Command = command;
            item.IsEnabled = isEnabled;
            SemanticProperties.SetDescription(item, semanticDescription ?? string.Empty);
        }

        private static string ResolveText(string text, string fallbackText)
        {
            return string.IsNullOrWhiteSpace(text)
                ? fallbackText
                : text;
        }

        private void CompleteNavigationVisibility()
        {
            IsVisible = IsNavigationVisible;
            UpdateInputTransparency();
        }

        private void UpdateInputTransparency()
        {
            InputTransparent = !IsVisible || !IsNavigationVisible;
        }
    }
}
