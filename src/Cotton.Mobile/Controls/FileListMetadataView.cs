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
        private bool _hasAppliedTrailingChipVisibility;

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
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? DefaultGridStyleResourceKey
                : GridStyleResourceKey;
            string titleStyleResourceKey = string.IsNullOrWhiteSpace(TitleStyleResourceKey)
                ? DefaultTitleStyleResourceKey
                : TitleStyleResourceKey;
            string detailStyleResourceKey = string.IsNullOrWhiteSpace(DetailStyleResourceKey)
                ? DefaultDetailStyleResourceKey
                : DetailStyleResourceKey;
            string trailingChipStyleResourceKey = string.IsNullOrWhiteSpace(TrailingChipStyleResourceKey)
                ? DefaultTrailingChipStyleResourceKey
                : TrailingChipStyleResourceKey;
            string trailingTextStyleResourceKey = string.IsNullOrWhiteSpace(TrailingTextStyleResourceKey)
                ? DefaultTrailingTextStyleResourceKey
                : TrailingTextStyleResourceKey;
            string trailingText = TrailingText ?? string.Empty;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _titleLabel.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _detailLabel.SetDynamicResource(StyleProperty, detailStyleResourceKey);
            _trailingChip.ChipStyleResourceKey = trailingChipStyleResourceKey;
            _trailingChip.LabelStyleResourceKey = trailingTextStyleResourceKey;

            _titleLabel.Text = Title ?? string.Empty;
            _detailLabel.Text = Detail ?? string.Empty;
            _trailingChip.Text = trailingText;
            UpdateTrailingChipVisibility(trailingText, animateTrailingChipVisibility);
        }

        private void UpdateTrailingChipVisibility(string trailingText, bool animateTrailingChipVisibility)
        {
            bool isTrailingChipVisible = IsTrailingChipVisible(trailingText);
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
            if (IsTrailingChipVisible(TrailingText ?? string.Empty))
            {
                _trailingChip.IsVisible = true;
                return;
            }

            _trailingChip.IsVisible = false;
        }

        private bool IsTrailingChipVisible(string trailingText)
        {
            return IsTrailingTextVisible && !string.IsNullOrWhiteSpace(trailingText);
        }
    }
}
