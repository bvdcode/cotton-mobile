// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SettingsSectionHeaderView : ContentView
    {
        private const string DefaultDetailTextStyleResourceKey = "M3CardSupportingBlock";
        private const string DefaultGridStyleResourceKey = "M3SettingsListItemGrid";
        private const string DefaultLeadingIconFrameStyleResourceKey = "M3CardUtilityThumbnailFrame";
        private const string DefaultTextStackStyleResourceKey = "M3CardTextStack";
        private const string DefaultTitleStyleResourceKey = "M3CardTitle";
        private const string DefaultTrailingChipStyleResourceKey = "M3NeutralChip";
        private const string DefaultTrailingTextStyleResourceKey = "M3ChipLabel";
        private const string PrimaryDetailTextOpacityAnimationName = "M3SettingsSectionPrimaryDetailOpacity";
        private const string ProgressOpacityAnimationName = "M3SettingsSectionProgressOpacity";
        private const string SecondaryDetailTextOpacityAnimationName = "M3SettingsSectionSecondaryDetailOpacity";
        private const string TertiaryDetailTextOpacityAnimationName = "M3SettingsSectionTertiaryDetailOpacity";
        private const string QuaternaryDetailTextOpacityAnimationName = "M3SettingsSectionQuaternaryDetailOpacity";
        private const string TrailingContentOpacityAnimationName = "M3SettingsSectionTrailingContentOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextProperty = BindableProperty.Create(
            nameof(PrimaryDetailText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnPrimaryDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextProperty = BindableProperty.Create(
            nameof(SecondaryDetailText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnSecondaryDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextProperty = BindableProperty.Create(
            nameof(TertiaryDetailText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnTertiaryDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty QuaternaryDetailTextProperty = BindableProperty.Create(
            nameof(QuaternaryDetailText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnQuaternaryDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
            nameof(Progress),
            typeof(double),
            typeof(SettingsSectionHeaderView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsProgressVisibleProperty = BindableProperty.Create(
            nameof(IsProgressVisible),
            typeof(bool),
            typeof(SettingsSectionHeaderView),
            false,
            propertyChanged: OnProgressVisibilityPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(SettingsSectionHeaderView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingContentProperty = BindableProperty.Create(
            nameof(TrailingContent),
            typeof(View),
            typeof(SettingsSectionHeaderView),
            propertyChanged: OnTrailingContentVisibilityPropertyChanged);

        public static readonly BindableProperty TrailingTextProperty = BindableProperty.Create(
            nameof(TrailingText),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            string.Empty,
            propertyChanged: OnTrailingContentVisibilityPropertyChanged);

        public static readonly BindableProperty IsTrailingTextVisibleProperty = BindableProperty.Create(
            nameof(IsTrailingTextVisible),
            typeof(bool),
            typeof(SettingsSectionHeaderView),
            false,
            propertyChanged: OnTrailingContentVisibilityPropertyChanged);

        public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(SettingsSectionHeaderView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TapCommandParameterProperty = BindableProperty.Create(
            nameof(TapCommandParameter),
            typeof(object),
            typeof(SettingsSectionHeaderView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsTapEnabledProperty = BindableProperty.Create(
            nameof(IsTapEnabled),
            typeof(bool),
            typeof(SettingsSectionHeaderView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultLeadingIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultTextStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultTitleStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TertiaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty QuaternaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(QuaternaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingChipStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultTrailingChipStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingTextStyleResourceKey),
            typeof(string),
            typeof(SettingsSectionHeaderView),
            DefaultTrailingTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _primaryDetailText;
        private readonly Label _quaternaryDetailText;
        private readonly Grid _grid;
        private readonly IconFrame _leadingIcon;
        private readonly LinearProgressView _progress;
        private readonly Label _secondaryDetailText;
        private readonly Label _tertiaryDetailText;
        private readonly VerticalStackLayout _textStack;
        private readonly Label _title;
        private readonly TouchSurfaceView _touchSurface;
        private readonly ChipView _trailingChip;
        private readonly ContentView _trailingContentHost;
        private bool _hasAppliedPrimaryDetailTextVisibility;
        private bool _hasAppliedProgressVisibility;
        private bool _hasAppliedSecondaryDetailTextVisibility;
        private bool _hasAppliedTertiaryDetailTextVisibility;
        private bool _hasAppliedQuaternaryDetailTextVisibility;
        private bool _hasAppliedTrailingContentVisibility;

        public SettingsSectionHeaderView()
        {
            _leadingIcon = new IconFrame();
            _title = new Label();
            _primaryDetailText = new Label();
            _secondaryDetailText = new Label();
            _tertiaryDetailText = new Label();
            _quaternaryDetailText = new Label();
            _progress = new LinearProgressView();
            _touchSurface = new TouchSurfaceView();
            _trailingChip = new ChipView();
            _trailingContentHost = new ContentView
            {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
            };
            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _title,
                    _primaryDetailText,
                    _secondaryDetailText,
                    _tertiaryDetailText,
                    _quaternaryDetailText,
                },
            };

            Grid.SetColumn(_textStack, 1);
            Grid.SetRow(_progress, 1);
            Grid.SetColumn(_progress, 1);
            Grid.SetColumn(_trailingContentHost, 2);
            Grid.SetRowSpan(_touchSurface, 2);
            Grid.SetColumnSpan(_touchSurface, 3);

            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    _leadingIcon,
                    _textStack,
                    _progress,
                    _touchSurface,
                    _trailingContentHost,
                },
            };

            Content = _grid;
            UpdateVisualState(
                animatePrimaryDetailTextVisibility: false,
                animateProgressVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateQuaternaryDetailTextVisibility: false,
                animateTrailingContentVisibility: false);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string PrimaryDetailText
        {
            get => (string)GetValue(PrimaryDetailTextProperty);
            set => SetValue(PrimaryDetailTextProperty, value);
        }

        public string SecondaryDetailText
        {
            get => (string)GetValue(SecondaryDetailTextProperty);
            set => SetValue(SecondaryDetailTextProperty, value);
        }

        public string TertiaryDetailText
        {
            get => (string)GetValue(TertiaryDetailTextProperty);
            set => SetValue(TertiaryDetailTextProperty, value);
        }

        public string QuaternaryDetailText
        {
            get => (string)GetValue(QuaternaryDetailTextProperty);
            set => SetValue(QuaternaryDetailTextProperty, value);
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public bool IsProgressVisible
        {
            get => (bool)GetValue(IsProgressVisibleProperty);
            set => SetValue(IsProgressVisibleProperty, value);
        }

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public View? TrailingContent
        {
            get => (View?)GetValue(TrailingContentProperty);
            set => SetValue(TrailingContentProperty, value);
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

        public ICommand? TapCommand
        {
            get => (ICommand?)GetValue(TapCommandProperty);
            set => SetValue(TapCommandProperty, value);
        }

        public object? TapCommandParameter
        {
            get => GetValue(TapCommandParameterProperty);
            set => SetValue(TapCommandParameterProperty, value);
        }

        public bool IsTapEnabled
        {
            get => (bool)GetValue(IsTapEnabledProperty);
            set => SetValue(IsTapEnabledProperty, value);
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

        public string TitleTextStyleResourceKey
        {
            get => (string)GetValue(TitleTextStyleResourceKeyProperty);
            set => SetValue(TitleTextStyleResourceKeyProperty, value);
        }

        public string PrimaryDetailTextStyleResourceKey
        {
            get => (string)GetValue(PrimaryDetailTextStyleResourceKeyProperty);
            set => SetValue(PrimaryDetailTextStyleResourceKeyProperty, value);
        }

        public string SecondaryDetailTextStyleResourceKey
        {
            get => (string)GetValue(SecondaryDetailTextStyleResourceKeyProperty);
            set => SetValue(SecondaryDetailTextStyleResourceKeyProperty, value);
        }

        public string TertiaryDetailTextStyleResourceKey
        {
            get => (string)GetValue(TertiaryDetailTextStyleResourceKeyProperty);
            set => SetValue(TertiaryDetailTextStyleResourceKeyProperty, value);
        }

        public string QuaternaryDetailTextStyleResourceKey
        {
            get => (string)GetValue(QuaternaryDetailTextStyleResourceKeyProperty);
            set => SetValue(QuaternaryDetailTextStyleResourceKeyProperty, value);
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
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState(
                animatePrimaryDetailTextVisibility: false,
                animateProgressVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateQuaternaryDetailTextVisibility: false,
                animateTrailingContentVisibility: false);
        }

        private static void OnPrimaryDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState(
                animatePrimaryDetailTextVisibility: true,
                animateProgressVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateQuaternaryDetailTextVisibility: false,
                animateTrailingContentVisibility: false);
        }

        private static void OnProgressVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState(
                animatePrimaryDetailTextVisibility: false,
                animateProgressVisibility: true,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateQuaternaryDetailTextVisibility: false,
                animateTrailingContentVisibility: false);
        }

        private static void OnSecondaryDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState(
                animatePrimaryDetailTextVisibility: false,
                animateProgressVisibility: false,
                animateSecondaryDetailTextVisibility: true,
                animateTertiaryDetailTextVisibility: false,
                animateQuaternaryDetailTextVisibility: false,
                animateTrailingContentVisibility: false);
        }

        private static void OnTertiaryDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState(
                animatePrimaryDetailTextVisibility: false,
                animateProgressVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: true,
                animateQuaternaryDetailTextVisibility: false,
                animateTrailingContentVisibility: false);
        }

        private static void OnQuaternaryDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState(
                animatePrimaryDetailTextVisibility: false,
                animateProgressVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateQuaternaryDetailTextVisibility: true,
                animateTrailingContentVisibility: false);
        }

        private static void OnTrailingContentVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsSectionHeaderView view = (SettingsSectionHeaderView)bindable;
            view.UpdateVisualState(
                animatePrimaryDetailTextVisibility: false,
                animateProgressVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateQuaternaryDetailTextVisibility: false,
                animateTrailingContentVisibility: true);
        }

        private void UpdateVisualState(
            bool animatePrimaryDetailTextVisibility,
            bool animateProgressVisibility,
            bool animateSecondaryDetailTextVisibility,
            bool animateTertiaryDetailTextVisibility,
            bool animateQuaternaryDetailTextVisibility,
            bool animateTrailingContentVisibility)
        {
            string title = Title ?? string.Empty;
            string primaryDetailText = PrimaryDetailText ?? string.Empty;
            string secondaryDetailText = SecondaryDetailText ?? string.Empty;
            string tertiaryDetailText = TertiaryDetailText ?? string.Empty;
            string quaternaryDetailText = QuaternaryDetailText ?? string.Empty;
            string trailingText = TrailingText ?? string.Empty;
            string gridStyleResourceKey = ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string leadingIconFrameStyleResourceKey =
                ResolveStyleResourceKey(LeadingIconFrameStyleResourceKey, DefaultLeadingIconFrameStyleResourceKey);
            string textStackStyleResourceKey =
                ResolveStyleResourceKey(TextStackStyleResourceKey, DefaultTextStackStyleResourceKey);
            string titleTextStyleResourceKey =
                ResolveStyleResourceKey(TitleTextStyleResourceKey, DefaultTitleStyleResourceKey);
            string primaryDetailTextStyleResourceKey =
                ResolveStyleResourceKey(PrimaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string secondaryDetailTextStyleResourceKey =
                ResolveStyleResourceKey(SecondaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string tertiaryDetailTextStyleResourceKey =
                ResolveStyleResourceKey(TertiaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string quaternaryDetailTextStyleResourceKey =
                ResolveStyleResourceKey(QuaternaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string trailingChipStyleResourceKey =
                ResolveStyleResourceKey(TrailingChipStyleResourceKey, DefaultTrailingChipStyleResourceKey);
            string trailingTextStyleResourceKey =
                ResolveStyleResourceKey(TrailingTextStyleResourceKey, DefaultTrailingTextStyleResourceKey);
            bool isLeadingIconVisible = LeadingIconData is not null;
            View? trailingContent = ResolveTrailingContent(trailingText);
            bool isTrailingContentVisible = trailingContent is not null;
            bool hasTrailingContentLayout = ResolveTrailingContentLayoutVisibility(
                isTrailingContentVisible,
                animateTrailingContentVisibility);
            ICommand? tapCommand = TapCommand;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            _leadingIcon.IconData = LeadingIconData;
            _leadingIcon.IsVisible = isLeadingIconVisible;
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _title.SetDynamicResource(StyleProperty, titleTextStyleResourceKey);
            _title.Text = title;
            _primaryDetailText.SetDynamicResource(StyleProperty, primaryDetailTextStyleResourceKey);
            _primaryDetailText.Text = primaryDetailText;
            UpdateDetailTextVisibility(
                _primaryDetailText,
                primaryDetailText,
                animatePrimaryDetailTextVisibility,
                ref _hasAppliedPrimaryDetailTextVisibility,
                PrimaryDetailTextOpacityAnimationName,
                CompletePrimaryDetailTextVisibility);
            _secondaryDetailText.SetDynamicResource(StyleProperty, secondaryDetailTextStyleResourceKey);
            _secondaryDetailText.Text = secondaryDetailText;
            UpdateDetailTextVisibility(
                _secondaryDetailText,
                secondaryDetailText,
                animateSecondaryDetailTextVisibility,
                ref _hasAppliedSecondaryDetailTextVisibility,
                SecondaryDetailTextOpacityAnimationName,
                CompleteSecondaryDetailTextVisibility);
            _tertiaryDetailText.SetDynamicResource(StyleProperty, tertiaryDetailTextStyleResourceKey);
            _tertiaryDetailText.Text = tertiaryDetailText;
            UpdateDetailTextVisibility(
                _tertiaryDetailText,
                tertiaryDetailText,
                animateTertiaryDetailTextVisibility,
                ref _hasAppliedTertiaryDetailTextVisibility,
                TertiaryDetailTextOpacityAnimationName,
                CompleteTertiaryDetailTextVisibility);
            _quaternaryDetailText.SetDynamicResource(StyleProperty, quaternaryDetailTextStyleResourceKey);
            _quaternaryDetailText.Text = quaternaryDetailText;
            UpdateDetailTextVisibility(
                _quaternaryDetailText,
                quaternaryDetailText,
                animateQuaternaryDetailTextVisibility,
                ref _hasAppliedQuaternaryDetailTextVisibility,
                QuaternaryDetailTextOpacityAnimationName,
                CompleteQuaternaryDetailTextVisibility);
            _progress.Progress = Progress;
            UpdateProgressVisibility(animateProgressVisibility);
            _touchSurface.TapCommand = IsTapEnabled ? tapCommand : null;
            _touchSurface.TapCommandParameter = TapCommandParameter;
            _touchSurface.IsVisible = IsTapEnabled && tapCommand is not null;
            _trailingChip.Text = trailingText;
            _trailingChip.ChipStyleResourceKey = trailingChipStyleResourceKey;
            _trailingChip.LabelStyleResourceKey = trailingTextStyleResourceKey;

            if (isTrailingContentVisible && _trailingContentHost.Content != trailingContent)
            {
                _trailingContentHost.Content = trailingContent;
            }

            UpdateTrailingContentVisibility(isTrailingContentVisible, animateTrailingContentVisibility);

            Grid.SetColumn(_textStack, isLeadingIconVisible ? 1 : 0);
            Grid.SetColumnSpan(_textStack, ResolveContentColumnSpan(isLeadingIconVisible, hasTrailingContentLayout));
            Grid.SetColumn(_progress, isLeadingIconVisible ? 1 : 0);
            Grid.SetColumnSpan(_progress, ResolveContentColumnSpan(isLeadingIconVisible, hasTrailingContentLayout));

            SemanticProperties.SetDescription(
                this,
                CreateSemanticDescription(
                    title,
                    primaryDetailText,
                    secondaryDetailText,
                    tertiaryDetailText,
                    quaternaryDetailText));
        }

        private void UpdateDetailTextVisibility(
            Label detailTextLabel,
            string detailText,
            bool animateDetailTextVisibility,
            ref bool hasAppliedDetailTextVisibility,
            string animationName,
            Action completeVisibility)
        {
            bool isDetailTextVisible = IsDetailTextActuallyVisible(detailText);
            bool shouldAnimate = animateDetailTextVisibility && hasAppliedDetailTextVisibility;
            double targetOpacity = isDetailTextVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isDetailTextVisible)
            {
                detailTextLabel.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                detailTextLabel,
                detailTextLabel.Opacity,
                targetOpacity,
                duration,
                animationName,
                shouldAnimate,
                opacity => detailTextLabel.Opacity = opacity,
                completeVisibility);
            hasAppliedDetailTextVisibility = true;
        }

        private void UpdateProgressVisibility(bool animateProgressVisibility)
        {
            bool shouldAnimate = animateProgressVisibility && _hasAppliedProgressVisibility;
            double targetOpacity = IsProgressVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (IsProgressVisible)
            {
                _progress.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _progress,
                _progress.Opacity,
                targetOpacity,
                duration,
                ProgressOpacityAnimationName,
                shouldAnimate,
                opacity => _progress.Opacity = opacity,
                CompleteProgressVisibility);
            _hasAppliedProgressVisibility = true;
        }

        private void UpdateTrailingContentVisibility(
            bool isTrailingContentVisible,
            bool animateTrailingContentVisibility)
        {
            bool shouldAnimate = animateTrailingContentVisibility && _hasAppliedTrailingContentVisibility;
            double targetOpacity = isTrailingContentVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isTrailingContentVisible)
            {
                _trailingContentHost.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _trailingContentHost,
                _trailingContentHost.Opacity,
                targetOpacity,
                duration,
                TrailingContentOpacityAnimationName,
                shouldAnimate,
                opacity => _trailingContentHost.Opacity = opacity,
                CompleteTrailingContentVisibility);
            _hasAppliedTrailingContentVisibility = true;
        }

        private void CompleteProgressVisibility()
        {
            if (IsProgressVisible)
            {
                _progress.IsVisible = true;
                return;
            }

            _progress.IsVisible = false;
        }

        private void CompleteTrailingContentVisibility()
        {
            View? trailingContent = ResolveTrailingContent(TrailingText ?? string.Empty);
            if (trailingContent is not null)
            {
                if (_trailingContentHost.Content != trailingContent)
                {
                    _trailingContentHost.Content = trailingContent;
                }

                _trailingContentHost.IsVisible = true;
                return;
            }

            _trailingContentHost.Content = null;
            _trailingContentHost.IsVisible = false;
        }

        private void CompletePrimaryDetailTextVisibility()
        {
            if (IsDetailTextActuallyVisible(PrimaryDetailText ?? string.Empty))
            {
                _primaryDetailText.IsVisible = true;
                return;
            }

            _primaryDetailText.IsVisible = false;
        }

        private void CompleteSecondaryDetailTextVisibility()
        {
            if (IsDetailTextActuallyVisible(SecondaryDetailText ?? string.Empty))
            {
                _secondaryDetailText.IsVisible = true;
                return;
            }

            _secondaryDetailText.IsVisible = false;
        }

        private void CompleteTertiaryDetailTextVisibility()
        {
            if (IsDetailTextActuallyVisible(TertiaryDetailText ?? string.Empty))
            {
                _tertiaryDetailText.IsVisible = true;
                return;
            }

            _tertiaryDetailText.IsVisible = false;
        }

        private void CompleteQuaternaryDetailTextVisibility()
        {
            if (IsDetailTextActuallyVisible(QuaternaryDetailText ?? string.Empty))
            {
                _quaternaryDetailText.IsVisible = true;
                return;
            }

            _quaternaryDetailText.IsVisible = false;
        }

        private static bool IsDetailTextActuallyVisible(string detailText)
        {
            return !string.IsNullOrWhiteSpace(detailText);
        }

        private View? ResolveTrailingContent(string trailingText)
        {
            if (TrailingContent is not null)
            {
                return TrailingContent;
            }

            return IsTrailingTextVisible && !string.IsNullOrWhiteSpace(trailingText) ? _trailingChip : null;
        }

        private bool ResolveTrailingContentLayoutVisibility(
            bool isTrailingContentVisible,
            bool animateTrailingContentVisibility)
        {
            if (isTrailingContentVisible)
            {
                return true;
            }

            return animateTrailingContentVisibility
                && _hasAppliedTrailingContentVisibility
                && _trailingContentHost.IsVisible;
        }

        private static string ResolveStyleResourceKey(string resourceKey, string defaultResourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey) ? defaultResourceKey : resourceKey;
        }

        private static int ResolveContentColumnSpan(bool isLeadingIconVisible, bool isTrailingContentVisible)
        {
            if (isLeadingIconVisible)
            {
                return isTrailingContentVisible ? 1 : 2;
            }

            return isTrailingContentVisible ? 2 : 3;
        }

        private static string CreateSemanticDescription(
            string title,
            string primaryDetailText,
            string secondaryDetailText,
            string tertiaryDetailText,
            string quaternaryDetailText)
        {
            List<string> parts =
            [
                title,
                primaryDetailText,
                secondaryDetailText,
                tertiaryDetailText,
                quaternaryDetailText,
            ];
            return string.Join(". ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
