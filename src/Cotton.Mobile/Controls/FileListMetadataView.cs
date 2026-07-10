// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class FileListMetadataView : ContentView
    {
        private const string DefaultDetailStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultGridStyleResourceKey = "M3FileListMetadataGrid";
        private const string DefaultTitleStyleResourceKey = "M3CardTitle";
        private const string DefaultTrailingChipStyleResourceKey = "M3NeutralChip";
        private const string DefaultTrailingTextStyleResourceKey = "M3ChipLabel";
        private const string TrailingChipOpacityAnimationName = "M3FileListTrailingChipOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(FileListMetadataView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailProperty = BindableProperty.Create(
            nameof(Detail),
            typeof(string),
            typeof(FileListMetadataView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextProperty = BindableProperty.Create(
            nameof(TrailingText),
            typeof(string),
            typeof(FileListMetadataView),
            string.Empty,
            propertyChanged: OnTrailingChipVisibilityPropertyChanged);

        public static readonly BindableProperty IsTrailingTextVisibleProperty = BindableProperty.Create(
            nameof(IsTrailingTextVisible),
            typeof(bool),
            typeof(FileListMetadataView),
            false,
            propertyChanged: OnTrailingChipVisibilityPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(FileListMetadataView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(FileListMetadataView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailStyleResourceKey),
            typeof(string),
            typeof(FileListMetadataView),
            DefaultDetailStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingChipStyleResourceKey),
            typeof(string),
            typeof(FileListMetadataView),
            DefaultTrailingChipStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingTextStyleResourceKey),
            typeof(string),
            typeof(FileListMetadataView),
            DefaultTrailingTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _detailLabel;
        private readonly Grid _grid;
        private readonly Label _titleLabel;
        private readonly ChipView _trailingChip;
        private string? _appliedDetailStyleResourceKey;
        private string? _appliedGridStyleResourceKey;
        private string? _appliedTitleStyleResourceKey;
        private bool _hasAppliedTrailingChipVisibility;
        private bool _isCurrentTrailingTextVisible;
        private string _currentTrailingText = string.Empty;

        public FileListMetadataView()
        {
            InputTransparent = true;

            _titleLabel = new Label();
            _detailLabel = new Label();
            _trailingChip = new ChipView();

            Grid.SetRow(_detailLabel, 1);
            Grid.SetColumnSpan(_detailLabel, 2);
            Grid.SetColumn(_trailingChip, 1);

            _grid = new Grid
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
                    _trailingChip,
                },
            };

            Content = _grid;
            UpdateVisualState(animateTrailingChipVisibility: false);
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

        public string TrailingText
        {
            get => (string)GetValue(TrailingTextProperty);
            set => SetValue(TrailingTextProperty, value);
        }

        public bool IsTrailingTextVisible
        {
            get => (bool)GetValue(IsTrailingTextVisibleProperty);
            set => SetValue(IsTrailingTextVisibleProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string TitleStyleResourceKey
        {
            get => (string)GetValue(TitleStyleResourceKeyProperty);
            set => SetValue(TitleStyleResourceKeyProperty, value);
        }

        public string DetailStyleResourceKey
        {
            get => (string)GetValue(DetailStyleResourceKeyProperty);
            set => SetValue(DetailStyleResourceKeyProperty, value);
        }

        public string TrailingChipStyleResourceKey
        {
            get => (string)GetValue(TrailingChipStyleResourceKeyProperty);
            set => SetValue(TrailingChipStyleResourceKeyProperty, value);
        }

        public string TrailingTextStyleResourceKey
        {
            get => (string)GetValue(TrailingTextStyleResourceKeyProperty);
            set => SetValue(TrailingTextStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileListMetadataView view = (FileListMetadataView)bindable;
            view.UpdateVisualState(animateTrailingChipVisibility: false);
        }

        private static void OnTrailingChipVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FileListMetadataView view = (FileListMetadataView)bindable;
            view.UpdateVisualState(animateTrailingChipVisibility: true);
        }

        private void UpdateVisualState(bool animateTrailingChipVisibility)
        {
            ApplyResolvedVisualState(
                Title ?? string.Empty,
                Detail ?? string.Empty,
                TrailingText ?? string.Empty,
                IsTrailingTextVisible,
                GridStyleResourceKey,
                TitleStyleResourceKey,
                DetailStyleResourceKey,
                TrailingChipStyleResourceKey,
                TrailingTextStyleResourceKey,
                animateTrailingChipVisibility);
        }

        internal void ApplyMetadataState(
            string title,
            string detail,
            string trailingText,
            bool isTrailingTextVisible,
            string trailingChipStyleResourceKey,
            string trailingTextStyleResourceKey,
            bool animateTrailingChipVisibility = true)
        {
            ApplyResolvedVisualState(
                title,
                detail,
                trailingText,
                isTrailingTextVisible,
                GridStyleResourceKey,
                TitleStyleResourceKey,
                DetailStyleResourceKey,
                trailingChipStyleResourceKey,
                trailingTextStyleResourceKey,
                animateTrailingChipVisibility);
        }

        private void ApplyResolvedVisualState(
            string title,
            string detail,
            string trailingText,
            bool isTrailingTextVisible,
            string requestedGridStyleResourceKey,
            string requestedTitleStyleResourceKey,
            string requestedDetailStyleResourceKey,
            string requestedTrailingChipStyleResourceKey,
            string requestedTrailingTextStyleResourceKey,
            bool animateTrailingChipVisibility)
        {
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedGridStyleResourceKey,
                DefaultGridStyleResourceKey);
            string titleStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTitleStyleResourceKey,
                DefaultTitleStyleResourceKey);
            string detailStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedDetailStyleResourceKey,
                DefaultDetailStyleResourceKey);
            string trailingChipStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTrailingChipStyleResourceKey,
                DefaultTrailingChipStyleResourceKey);
            string trailingTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTrailingTextStyleResourceKey,
                DefaultTrailingTextStyleResourceKey);
            string currentTitle = title ?? string.Empty;
            string currentDetail = detail ?? string.Empty;
            string currentTrailingText = trailingText ?? string.Empty;

            ApplyStyleIfChanged(_grid, gridStyleResourceKey, ref _appliedGridStyleResourceKey);
            ApplyStyleIfChanged(_titleLabel, titleStyleResourceKey, ref _appliedTitleStyleResourceKey);
            ApplyStyleIfChanged(_detailLabel, detailStyleResourceKey, ref _appliedDetailStyleResourceKey);
            if (!string.Equals(_titleLabel.Text, currentTitle, StringComparison.Ordinal))
            {
                _titleLabel.Text = currentTitle;
            }

            if (!string.Equals(_detailLabel.Text, currentDetail, StringComparison.Ordinal))
            {
                _detailLabel.Text = currentDetail;
            }

            bool wasTrailingChipVisible = IsTrailingChipActuallyVisible(
                _currentTrailingText,
                _isCurrentTrailingTextVisible);
            bool isTrailingChipVisible = IsTrailingChipActuallyVisible(
                currentTrailingText,
                isTrailingTextVisible);
            _trailingChip.ApplyChipState(
                currentTrailingText,
                trailingChipStyleResourceKey,
                trailingTextStyleResourceKey,
                animateTextVisibility: false);
            _currentTrailingText = currentTrailingText;
            _isCurrentTrailingTextVisible = isTrailingTextVisible;
            if (!_hasAppliedTrailingChipVisibility || wasTrailingChipVisible != isTrailingChipVisible)
            {
                UpdateTrailingChipVisibility(
                    currentTrailingText,
                    isTrailingTextVisible,
                    animateTrailingChipVisibility);
            }
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

        private void UpdateTrailingChipVisibility(
            string trailingText,
            bool isTrailingTextVisible,
            bool animateTrailingChipVisibility)
        {
            bool isTrailingChipVisible = IsTrailingChipActuallyVisible(trailingText, isTrailingTextVisible);
            bool shouldAnimate = animateTrailingChipVisibility && _hasAppliedTrailingChipVisibility;
            double targetOpacity = isTrailingChipVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isTrailingChipVisible)
            {
                _trailingChip.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _trailingChip,
                _trailingChip.Opacity,
                targetOpacity,
                duration,
                TrailingChipOpacityAnimationName,
                shouldAnimate,
                opacity => _trailingChip.Opacity = opacity,
                CompleteTrailingChipVisibility);
            _hasAppliedTrailingChipVisibility = true;
        }

        private void CompleteTrailingChipVisibility()
        {
            if (IsTrailingChipActuallyVisible(_currentTrailingText, _isCurrentTrailingTextVisible))
            {
                _trailingChip.IsVisible = true;
                return;
            }

            _trailingChip.IsVisible = false;
        }

        private static bool IsTrailingChipActuallyVisible(string trailingText, bool isTrailingTextVisible)
        {
            return isTrailingTextVisible && !string.IsNullOrWhiteSpace(trailingText);
        }
    }
}
