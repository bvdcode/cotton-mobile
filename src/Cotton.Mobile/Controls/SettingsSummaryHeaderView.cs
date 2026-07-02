// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class SettingsSummaryHeaderView : ContentView
    {
        private const string DefaultDetailStyleResourceKey = "M3CardSupportingBlock";
        private const string DefaultGridStyleResourceKey = "M3SettingsSummaryGrid";
        private const string DefaultStatusStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultTitleStyleResourceKey = "M3CardTitle";
        private const string DetailOpacityAnimationName = "M3SettingsSummaryDetailOpacity";
        private const string StatusOpacityAnimationName = "M3SettingsSummaryStatusOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusTextProperty = BindableProperty.Create(
            nameof(StatusText),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(SettingsSummaryHeaderView),
            true,
            propertyChanged: OnStatusVisiblePropertyChanged);

        public static readonly BindableProperty IsDetailVisibleProperty = BindableProperty.Create(
            nameof(IsDetailVisible),
            typeof(bool),
            typeof(SettingsSummaryHeaderView),
            true,
            propertyChanged: OnDetailVisiblePropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StatusStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultStatusStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailStyleResourceKey),
            typeof(string),
            typeof(SettingsSummaryHeaderView),
            DefaultDetailStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _detailLabel;
        private readonly Grid _grid;
        private readonly Label _statusLabel;
        private readonly Label _titleLabel;
        private bool _hasAppliedDetailVisibility;
        private bool _hasAppliedStatusVisibility;

        public SettingsSummaryHeaderView()
        {
            InputTransparent = true;

            _titleLabel = new Label();
            _statusLabel = new Label();
            _detailLabel = new Label();
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
                    _statusLabel,
                    _detailLabel,
                },
            };

            Grid.SetColumn(_statusLabel, 1);
            Grid.SetRow(_detailLabel, 1);
            Grid.SetColumnSpan(_detailLabel, 2);

            Content = _grid;
            UpdateVisualState(animateStatusVisibility: false, animateDetailVisibility: false);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public string DetailText
        {
            get => (string)GetValue(DetailTextProperty);
            set => SetValue(DetailTextProperty, value);
        }

        public bool IsStatusVisible
        {
            get => (bool)GetValue(IsStatusVisibleProperty);
            set => SetValue(IsStatusVisibleProperty, value);
        }

        public bool IsDetailVisible
        {
            get => (bool)GetValue(IsDetailVisibleProperty);
            set => SetValue(IsDetailVisibleProperty, value);
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

        public string StatusStyleResourceKey
        {
            get => (string)GetValue(StatusStyleResourceKeyProperty);
            set => SetValue(StatusStyleResourceKeyProperty, value);
        }

        public string DetailStyleResourceKey
        {
            get => (string)GetValue(DetailStyleResourceKeyProperty);
            set => SetValue(DetailStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsSummaryHeaderView view = (SettingsSummaryHeaderView)bindable;
            view.UpdateVisualState(animateStatusVisibility: false, animateDetailVisibility: false);
        }

        private static void OnStatusVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsSummaryHeaderView view = (SettingsSummaryHeaderView)bindable;
            view.UpdateVisualState(animateStatusVisibility: true, animateDetailVisibility: false);
        }

        private static void OnDetailVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsSummaryHeaderView view = (SettingsSummaryHeaderView)bindable;
            view.UpdateVisualState(animateStatusVisibility: false, animateDetailVisibility: true);
        }

        private void UpdateVisualState(bool animateStatusVisibility, bool animateDetailVisibility)
        {
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? DefaultGridStyleResourceKey
                : GridStyleResourceKey;
            string titleStyleResourceKey = string.IsNullOrWhiteSpace(TitleStyleResourceKey)
                ? DefaultTitleStyleResourceKey
                : TitleStyleResourceKey;
            string statusStyleResourceKey = string.IsNullOrWhiteSpace(StatusStyleResourceKey)
                ? DefaultStatusStyleResourceKey
                : StatusStyleResourceKey;
            string detailStyleResourceKey = string.IsNullOrWhiteSpace(DetailStyleResourceKey)
                ? DefaultDetailStyleResourceKey
                : DetailStyleResourceKey;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _titleLabel.SetDynamicResource(StyleProperty, titleStyleResourceKey);
            _statusLabel.SetDynamicResource(StyleProperty, statusStyleResourceKey);
            _detailLabel.SetDynamicResource(StyleProperty, detailStyleResourceKey);

            _titleLabel.Text = Title ?? string.Empty;
            _statusLabel.Text = StatusText ?? string.Empty;
            _detailLabel.Text = DetailText ?? string.Empty;
            UpdateStatusVisibility(animateStatusVisibility);
            UpdateDetailVisibility(animateDetailVisibility);
        }

        private void UpdateStatusVisibility(bool animateStatusVisibility)
        {
            bool isStatusVisible = IsStatusVisible;
            bool shouldAnimate = animateStatusVisibility && _hasAppliedStatusVisibility;
            double targetOpacity = isStatusVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isStatusVisible)
            {
                _statusLabel.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _statusLabel,
                _statusLabel.Opacity,
                targetOpacity,
                duration,
                StatusOpacityAnimationName,
                shouldAnimate,
                opacity => _statusLabel.Opacity = opacity,
                CompleteStatusVisibility);
            _hasAppliedStatusVisibility = true;
        }

        private void UpdateDetailVisibility(bool animateDetailVisibility)
        {
            bool isDetailVisible = IsDetailVisible;
            bool shouldAnimate = animateDetailVisibility && _hasAppliedDetailVisibility;
            double targetOpacity = isDetailVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isDetailVisible)
            {
                _detailLabel.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _detailLabel,
                _detailLabel.Opacity,
                targetOpacity,
                duration,
                DetailOpacityAnimationName,
                shouldAnimate,
                opacity => _detailLabel.Opacity = opacity,
                CompleteDetailVisibility);
            _hasAppliedDetailVisibility = true;
        }

        private void CompleteStatusVisibility()
        {
            if (IsStatusVisible)
            {
                _statusLabel.IsVisible = true;
                return;
            }

            _statusLabel.IsVisible = false;
        }

        private void CompleteDetailVisibility()
        {
            if (IsDetailVisible)
            {
                _detailLabel.IsVisible = true;
                return;
            }

            _detailLabel.IsVisible = false;
        }
    }
}
