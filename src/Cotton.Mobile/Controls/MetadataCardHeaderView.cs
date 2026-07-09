// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class MetadataCardHeaderView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3MetadataCardGrid";
        private const string DefaultLeadingIconFrameStyleResourceKey = "M3CardFileThumbnailFrame";
        private const string DefaultSupportingTextStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultTextStackStyleResourceKey = "M3CardTextStack";
        private const string DefaultTitleStyleResourceKey = "M3CardTitle";
        private const string DefaultTrailingChipStyleResourceKey = "M3NeutralChip";
        private const string DefaultTrailingTextStyleResourceKey = "M3ChipLabel";
        private const string TrailingChipOpacityAnimationName = "M3MetadataCardTrailingChipOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(MetadataCardHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
            nameof(SupportingText),
            typeof(string),
            typeof(MetadataCardHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextProperty = BindableProperty.Create(
            nameof(TrailingText),
            typeof(string),
            typeof(MetadataCardHeaderView),
            string.Empty,
            propertyChanged: OnTrailingChipVisibilityPropertyChanged);

        public static readonly BindableProperty IsTrailingTextVisibleProperty = BindableProperty.Create(
            nameof(IsTrailingTextVisible),
            typeof(bool),
            typeof(MetadataCardHeaderView),
            true,
            propertyChanged: OnTrailingChipVisibilityPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(MetadataCardHeaderView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(MetadataCardHeaderView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(MetadataCardHeaderView),
            DefaultLeadingIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(MetadataCardHeaderView),
            DefaultTextStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(MetadataCardHeaderView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SupportingTextStyleResourceKey),
            typeof(string),
            typeof(MetadataCardHeaderView),
            DefaultSupportingTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingChipStyleResourceKey),
            typeof(string),
            typeof(MetadataCardHeaderView),
            DefaultTrailingChipStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingTextStyleResourceKey),
            typeof(string),
            typeof(MetadataCardHeaderView),
            DefaultTrailingTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;
        private readonly IconFrame _leadingIcon;
        private readonly FileEntryTextView _textBlock;
        private readonly ChipView _trailingChip;
        private bool _hasAppliedTrailingChipVisibility;
        private bool _isCurrentTrailingTextVisible = true;
        private string _currentTrailingText = string.Empty;

        public MetadataCardHeaderView()
        {
            InputTransparent = true;

            _leadingIcon = new IconFrame();
            _textBlock = new FileEntryTextView();
            _trailingChip = new ChipView();

            Grid.SetColumn(_textBlock, 1);
            Grid.SetColumn(_trailingChip, 2);

            _grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _leadingIcon,
                    _textBlock,
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

        public string SupportingText
        {
            get => (string)GetValue(SupportingTextProperty);
            set => SetValue(SupportingTextProperty, value);
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

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string LeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(LeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(LeadingIconFrameStyleResourceKeyProperty, value);
        }

        public string TextStackStyleResourceKey
        {
            get => (string)GetValue(TextStackStyleResourceKeyProperty);
            set => SetValue(TextStackStyleResourceKeyProperty, value);
        }

        public string TitleStyleResourceKey
        {
            get => (string)GetValue(TitleStyleResourceKeyProperty);
            set => SetValue(TitleStyleResourceKeyProperty, value);
        }

        public string SupportingTextStyleResourceKey
        {
            get => (string)GetValue(SupportingTextStyleResourceKeyProperty);
            set => SetValue(SupportingTextStyleResourceKeyProperty, value);
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
            MetadataCardHeaderView view = (MetadataCardHeaderView)bindable;
            view.UpdateVisualState(animateTrailingChipVisibility: false);
        }

        private static void OnTrailingChipVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MetadataCardHeaderView view = (MetadataCardHeaderView)bindable;
            view.UpdateVisualState(animateTrailingChipVisibility: true);
        }

        private void UpdateVisualState(bool animateTrailingChipVisibility)
        {
            ApplyResolvedVisualState(
                Title ?? string.Empty,
                SupportingText ?? string.Empty,
                TrailingText ?? string.Empty,
                IsTrailingTextVisible,
                LeadingIconData,
                GridStyleResourceKey,
                LeadingIconFrameStyleResourceKey,
                TextStackStyleResourceKey,
                TitleStyleResourceKey,
                SupportingTextStyleResourceKey,
                TrailingChipStyleResourceKey,
                TrailingTextStyleResourceKey,
                animateTrailingChipVisibility);
        }

        internal void ApplyHeaderState(
            string title,
            string supportingText,
            string trailingText,
            bool isTrailingTextVisible,
            Geometry? leadingIconData,
            string leadingIconFrameStyleResourceKey)
        {
            ApplyResolvedVisualState(
                title,
                supportingText,
                trailingText,
                isTrailingTextVisible,
                leadingIconData,
                GridStyleResourceKey,
                leadingIconFrameStyleResourceKey,
                TextStackStyleResourceKey,
                TitleStyleResourceKey,
                SupportingTextStyleResourceKey,
                TrailingChipStyleResourceKey,
                TrailingTextStyleResourceKey,
                animateTrailingChipVisibility: true);
        }

        private void ApplyResolvedVisualState(
            string title,
            string supportingText,
            string trailingText,
            bool isTrailingTextVisible,
            Geometry? leadingIconData,
            string requestedGridStyleResourceKey,
            string requestedLeadingIconFrameStyleResourceKey,
            string requestedTextStackStyleResourceKey,
            string requestedTitleStyleResourceKey,
            string requestedSupportingTextStyleResourceKey,
            string requestedTrailingChipStyleResourceKey,
            string requestedTrailingTextStyleResourceKey,
            bool animateTrailingChipVisibility)
        {
            string resolvedGridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedGridStyleResourceKey,
                DefaultGridStyleResourceKey);
            string resolvedLeadingIconFrameStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedLeadingIconFrameStyleResourceKey,
                DefaultLeadingIconFrameStyleResourceKey);
            string resolvedTextStackStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTextStackStyleResourceKey,
                DefaultTextStackStyleResourceKey);
            string resolvedTitleStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTitleStyleResourceKey,
                DefaultTitleStyleResourceKey);
            string resolvedSupportingTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedSupportingTextStyleResourceKey,
                DefaultSupportingTextStyleResourceKey);
            string resolvedTrailingChipStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTrailingChipStyleResourceKey,
                DefaultTrailingChipStyleResourceKey);
            string resolvedTrailingTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                requestedTrailingTextStyleResourceKey,
                DefaultTrailingTextStyleResourceKey);
            string currentTitle = title ?? string.Empty;
            string currentSupportingText = supportingText ?? string.Empty;
            string currentTrailingText = trailingText ?? string.Empty;

            _grid.SetDynamicResource(StyleProperty, resolvedGridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, resolvedLeadingIconFrameStyleResourceKey);
            _leadingIcon.IconData = leadingIconData;
            _textBlock.Title = currentTitle;
            _textBlock.Detail = currentSupportingText;
            _textBlock.StackStyleResourceKey = resolvedTextStackStyleResourceKey;
            _textBlock.TitleStyleResourceKey = resolvedTitleStyleResourceKey;
            _textBlock.DetailStyleResourceKey = resolvedSupportingTextStyleResourceKey;
            _trailingChip.Text = currentTrailingText;
            _trailingChip.ChipStyleResourceKey = resolvedTrailingChipStyleResourceKey;
            _trailingChip.LabelStyleResourceKey = resolvedTrailingTextStyleResourceKey;
            _currentTrailingText = currentTrailingText;
            _isCurrentTrailingTextVisible = isTrailingTextVisible;
            UpdateTrailingChipVisibility(currentTrailingText, isTrailingTextVisible, animateTrailingChipVisibility);
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
