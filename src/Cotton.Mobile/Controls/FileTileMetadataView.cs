// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;

namespace Cotton.Mobile.Controls
{
    public class FileTileMetadataView : ContentView
    {
        private const string DefaultDetailStyleResourceKey = "M3CardMetaLine";
        private const string DefaultLocalChipLabelStyleResourceKey = "M3LocalCopyChipLabel";
        private const string DefaultLocalChipStyleResourceKey = "M3LocalCopyChip";
        private const string DefaultMetadataGridStyleResourceKey = "M3FileTileMetadataGrid";
        private const string DefaultOfflineChipLabelStyleResourceKey = "M3ErrorChipLabel";
        private const string DefaultOfflineChipStyleResourceKey = "M3FileAttentionChip";
        private const string DefaultStackStyleResourceKey = "M3FileTileTextStack";
        private const string DefaultTitleStyleResourceKey = "M3CardSupportingStrongLine";
        private const string LocalCopyChipOpacityAnimationName = "M3FileTileLocalChipOpacity";
        private const string OfflineAttentionChipOpacityAnimationName = "M3FileTileOfflineChipOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FileTileMetadataView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailProperty = BindableProperty.Create(
            nameof(Detail),
            typeof(string),
            typeof(FileTileMetadataView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LocalCopyStatusProperty = BindableProperty.Create(
            nameof(LocalCopyStatus),
            typeof(string),
            typeof(FileTileMetadataView),
            string.Empty,
            propertyChanged: OnLocalCopyChipVisibilityPropertyChanged);

        public static readonly BindableProperty IsLocalCopyVisibleProperty = BindableProperty.Create(
            nameof(IsLocalCopyVisible),
            typeof(bool),
            typeof(FileTileMetadataView),
            false,
            propertyChanged: OnLocalCopyChipVisibilityPropertyChanged);

        public static readonly BindableProperty OfflineAttentionStatusProperty = BindableProperty.Create(
            nameof(OfflineAttentionStatus),
            typeof(string),
            typeof(FileTileMetadataView),
            string.Empty,
            propertyChanged: OnOfflineAttentionChipVisibilityPropertyChanged);

        public static readonly BindableProperty IsOfflineAttentionVisibleProperty = BindableProperty.Create(
            nameof(IsOfflineAttentionVisible),
            typeof(bool),
            typeof(FileTileMetadataView),
            false,
            propertyChanged: OnOfflineAttentionChipVisibilityPropertyChanged);

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty MetadataGridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(MetadataGridStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultMetadataGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultDetailStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LocalChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LocalChipStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultLocalChipStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LocalChipLabelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LocalChipLabelStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultLocalChipLabelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty OfflineChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(OfflineChipStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultOfflineChipStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty OfflineChipLabelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(OfflineChipLabelStyleResourceKey),
            typeof(string),
            typeof(FileTileMetadataView),
            DefaultOfflineChipLabelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _detailLabel;
        private readonly ChipView _localCopyChip;
        private readonly Grid _metadataGrid;
        private readonly ChipView _offlineAttentionChip;
        private readonly VerticalStackLayout _stack;
        private readonly Label _titleLabel;
        private bool _hasAppliedLocalCopyChipVisibility;
        private bool _hasAppliedOfflineAttentionChipVisibility;
        private bool _isCurrentLocalCopyVisible;
        private bool _isCurrentOfflineAttentionVisible;
        private string _currentLocalCopyStatus = string.Empty;
        private string _currentOfflineAttentionStatus = string.Empty;

        public FileTileMetadataView()
        {
            InputTransparent = true;

            _titleLabel = new Label();
            _detailLabel = new Label();
            _localCopyChip = new ChipView();
            _offlineAttentionChip = new ChipView();
            _metadataGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _detailLabel,
                    _localCopyChip,
                    _offlineAttentionChip,
                },
            };
            _stack = new VerticalStackLayout
            {
                Children =
                {
                    _titleLabel,
                    _metadataGrid,
                },
            };

            Grid.SetColumn(_localCopyChip, 1);
            Grid.SetColumn(_offlineAttentionChip, 2);

            Content = _stack;
            UpdateVisualState(animateLocalCopyChipVisibility: false, animateOfflineAttentionChipVisibility: false);
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

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        public string TitleStyleResourceKey
        {
            get => (string)GetValue(TitleStyleResourceKeyProperty);
            set => SetValue(TitleStyleResourceKeyProperty, value);
        }

        public string MetadataGridStyleResourceKey
        {
            get => (string)GetValue(MetadataGridStyleResourceKeyProperty);
            set => SetValue(MetadataGridStyleResourceKeyProperty, value);
        }

        public string DetailStyleResourceKey
        {
            get => (string)GetValue(DetailStyleResourceKeyProperty);
            set => SetValue(DetailStyleResourceKeyProperty, value);
        }

        public string LocalChipStyleResourceKey
        {
            get => (string)GetValue(LocalChipStyleResourceKeyProperty);
            set => SetValue(LocalChipStyleResourceKeyProperty, value);
        }

        public string LocalChipLabelStyleResourceKey
        {
            get => (string)GetValue(LocalChipLabelStyleResourceKeyProperty);
            set => SetValue(LocalChipLabelStyleResourceKeyProperty, value);
        }

        public string OfflineChipStyleResourceKey
        {
            get => (string)GetValue(OfflineChipStyleResourceKeyProperty);
            set => SetValue(OfflineChipStyleResourceKeyProperty, value);
        }

        public string OfflineChipLabelStyleResourceKey
        {
            get => (string)GetValue(OfflineChipLabelStyleResourceKeyProperty);
            set => SetValue(OfflineChipLabelStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileTileMetadataView view = (FileTileMetadataView)bindable;
            view.UpdateVisualState(animateLocalCopyChipVisibility: false, animateOfflineAttentionChipVisibility: false);
        }

        private static void OnLocalCopyChipVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            FileTileMetadataView view = (FileTileMetadataView)bindable;
            view.UpdateVisualState(animateLocalCopyChipVisibility: true, animateOfflineAttentionChipVisibility: false);
        }

        private static void OnOfflineAttentionChipVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            FileTileMetadataView view = (FileTileMetadataView)bindable;
            view.UpdateVisualState(animateLocalCopyChipVisibility: false, animateOfflineAttentionChipVisibility: true);
        }

        private void UpdateVisualState(bool animateLocalCopyChipVisibility, bool animateOfflineAttentionChipVisibility)
        {
            ApplyResolvedVisualState(
                Title ?? string.Empty,
                Detail ?? string.Empty,
                LocalCopyStatus ?? string.Empty,
                IsLocalCopyVisible,
                OfflineAttentionStatus ?? string.Empty,
                IsOfflineAttentionVisible,
                StackStyleResourceKey,
                TitleStyleResourceKey,
                MetadataGridStyleResourceKey,
                DetailStyleResourceKey,
                LocalChipStyleResourceKey,
                LocalChipLabelStyleResourceKey,
                OfflineChipStyleResourceKey,
                OfflineChipLabelStyleResourceKey,
                animateLocalCopyChipVisibility,
                animateOfflineAttentionChipVisibility);
        }

        internal void ApplyMetadataState(
            string title,
            string detail,
            string localCopyStatus,
            bool isLocalCopyVisible,
            string offlineAttentionStatus,
            bool isOfflineAttentionVisible)
        {
            ApplyResolvedVisualState(
                title,
                detail,
                localCopyStatus,
                isLocalCopyVisible,
                offlineAttentionStatus,
                isOfflineAttentionVisible,
                StackStyleResourceKey,
                TitleStyleResourceKey,
                MetadataGridStyleResourceKey,
                DetailStyleResourceKey,
                LocalChipStyleResourceKey,
                LocalChipLabelStyleResourceKey,
                OfflineChipStyleResourceKey,
                OfflineChipLabelStyleResourceKey,
                animateLocalCopyChipVisibility: true,
                animateOfflineAttentionChipVisibility: true);
        }

        private void ApplyResolvedVisualState(
            string title,
            string detail,
            string localCopyStatus,
            bool isLocalCopyVisible,
            string offlineAttentionStatus,
            bool isOfflineAttentionVisible,
            string requestedStackStyleResourceKey,
            string requestedTitleStyleResourceKey,
            string requestedMetadataGridStyleResourceKey,
            string requestedDetailStyleResourceKey,
            string requestedLocalChipStyleResourceKey,
            string requestedLocalChipLabelStyleResourceKey,
            string requestedOfflineChipStyleResourceKey,
            string requestedOfflineChipLabelStyleResourceKey,
            bool animateLocalCopyChipVisibility,
            bool animateOfflineAttentionChipVisibility)
        {
            string stackStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedStackStyleResourceKey,
                DefaultStackStyleResourceKey);
            string titleStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTitleStyleResourceKey,
                DefaultTitleStyleResourceKey);
            string metadataGridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedMetadataGridStyleResourceKey,
                DefaultMetadataGridStyleResourceKey);
            string detailStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedDetailStyleResourceKey,
                DefaultDetailStyleResourceKey);
            string localChipStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedLocalChipStyleResourceKey,
                DefaultLocalChipStyleResourceKey);
            string localChipLabelStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedLocalChipLabelStyleResourceKey,
                DefaultLocalChipLabelStyleResourceKey);
            string offlineChipStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedOfflineChipStyleResourceKey,
                DefaultOfflineChipStyleResourceKey);
            string offlineChipLabelStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedOfflineChipLabelStyleResourceKey,
                DefaultOfflineChipLabelStyleResourceKey);
            string currentTitle = title ?? string.Empty;
            string currentDetail = detail ?? string.Empty;
            string currentLocalCopyStatus = localCopyStatus ?? string.Empty;
            string currentOfflineAttentionStatus = offlineAttentionStatus ?? string.Empty;

            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
            _titleLabel.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _metadataGrid.SetDynamicResource(StyleProperty, metadataGridStyleResourceKey);
            _detailLabel.SetDynamicResource(StyleProperty, detailStyleResourceKey);
            _localCopyChip.ChipStyleResourceKey = localChipStyleResourceKey;
            _localCopyChip.LabelStyleResourceKey = localChipLabelStyleResourceKey;
            _offlineAttentionChip.ChipStyleResourceKey = offlineChipStyleResourceKey;
            _offlineAttentionChip.LabelStyleResourceKey = offlineChipLabelStyleResourceKey;

            _titleLabel.Text = currentTitle;
            _detailLabel.Text = currentDetail;
            _localCopyChip.Text = currentLocalCopyStatus;
            _offlineAttentionChip.Text = currentOfflineAttentionStatus;
            _currentLocalCopyStatus = currentLocalCopyStatus;
            _isCurrentLocalCopyVisible = isLocalCopyVisible;
            _currentOfflineAttentionStatus = currentOfflineAttentionStatus;
            _isCurrentOfflineAttentionVisible = isOfflineAttentionVisible;
            UpdateLocalCopyChipVisibility(
                currentLocalCopyStatus,
                isLocalCopyVisible,
                animateLocalCopyChipVisibility);
            UpdateOfflineAttentionChipVisibility(
                currentOfflineAttentionStatus,
                isOfflineAttentionVisible,
                animateOfflineAttentionChipVisibility);
        }

        private void UpdateLocalCopyChipVisibility(
            string localCopyStatus,
            bool isLocalCopyVisible,
            bool animateLocalCopyChipVisibility)
        {
            UpdateChipVisibility(
                _localCopyChip,
                IsLocalCopyChipActuallyVisible(localCopyStatus, isLocalCopyVisible),
                animateLocalCopyChipVisibility,
                ref _hasAppliedLocalCopyChipVisibility,
                LocalCopyChipOpacityAnimationName,
                CompleteLocalCopyChipVisibility);
        }

        private void UpdateOfflineAttentionChipVisibility(
            string offlineAttentionStatus,
            bool isOfflineAttentionVisible,
            bool animateOfflineAttentionChipVisibility)
        {
            UpdateChipVisibility(
                _offlineAttentionChip,
                IsOfflineAttentionChipActuallyVisible(offlineAttentionStatus, isOfflineAttentionVisible),
                animateOfflineAttentionChipVisibility,
                ref _hasAppliedOfflineAttentionChipVisibility,
                OfflineAttentionChipOpacityAnimationName,
                CompleteOfflineAttentionChipVisibility);
        }

        private void UpdateChipVisibility(
            ChipView chip,
            bool isChipVisible,
            bool animateChipVisibility,
            ref bool hasAppliedChipVisibility,
            string animationName,
            Action completeVisibility)
        {
            bool shouldAnimate = animateChipVisibility && hasAppliedChipVisibility;
            double targetOpacity = isChipVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isChipVisible)
            {
                chip.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                chip,
                chip.Opacity,
                targetOpacity,
                duration,
                animationName,
                shouldAnimate,
                opacity => chip.Opacity = opacity,
                completeVisibility);
            hasAppliedChipVisibility = true;
        }

        private void CompleteLocalCopyChipVisibility()
        {
            if (IsLocalCopyChipActuallyVisible(_currentLocalCopyStatus, _isCurrentLocalCopyVisible))
            {
                _localCopyChip.IsVisible = true;
                return;
            }

            _localCopyChip.IsVisible = false;
        }

        private void CompleteOfflineAttentionChipVisibility()
        {
            if (IsOfflineAttentionChipActuallyVisible(
                _currentOfflineAttentionStatus,
                _isCurrentOfflineAttentionVisible))
            {
                _offlineAttentionChip.IsVisible = true;
                return;
            }

            _offlineAttentionChip.IsVisible = false;
        }

        private static bool IsLocalCopyChipActuallyVisible(string localCopyStatus, bool isLocalCopyVisible)
        {
            return isLocalCopyVisible && !string.IsNullOrWhiteSpace(localCopyStatus);
        }

        private static bool IsOfflineAttentionChipActuallyVisible(
            string offlineAttentionStatus,
            bool isOfflineAttentionVisible)
        {
            return isOfflineAttentionVisible && !string.IsNullOrWhiteSpace(offlineAttentionStatus);
        }
    }
}
