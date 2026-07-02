// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SettingsInfoItemView : ContentView
    {
        private const string DefaultAttentionLeadingIconFrameStyleResourceKey = "M3CardErrorThumbnailFrame";
        private const string DefaultAttentionTrailingTextStyleResourceKey = "M3ErrorChipLabel";
        private const string DefaultDetailTextStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultGridStyleResourceKey = "M3SettingsListItemGrid";
        private const string DefaultLeadingIconFrameStyleResourceKey = "M3CardUtilityThumbnailFrame";
        private const string DefaultTextStackStyleResourceKey = "M3CardTextStack";
        private const string DefaultTitleTextStyleResourceKey = "M3CardSupportingStrongLine";
        private const string DefaultTrailingChipStyleResourceKey = "M3TrailingChip";
        private const string DefaultTrailingTextStyleResourceKey = "M3ChipLabel";
        private const string LeadingIconOpacityAnimationName = "M3SettingsInfoLeadingIconOpacity";
        private const string PrimaryDetailTextOpacityAnimationName = "M3SettingsInfoPrimaryDetailOpacity";
        private const string SecondaryDetailTextOpacityAnimationName = "M3SettingsInfoSecondaryDetailOpacity";
        private const string TertiaryDetailTextOpacityAnimationName = "M3SettingsInfoTertiaryDetailOpacity";
        private const string TrailingChipOpacityAnimationName = "M3SettingsInfoTrailingChipOpacity";

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SettingsInfoItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextProperty = BindableProperty.Create(
            nameof(PrimaryDetailText),
            typeof(string),
            typeof(SettingsInfoItemView),
            string.Empty,
            propertyChanged: OnPrimaryDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextProperty = BindableProperty.Create(
            nameof(SecondaryDetailText),
            typeof(string),
            typeof(SettingsInfoItemView),
            string.Empty,
            propertyChanged: OnSecondaryDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextProperty = BindableProperty.Create(
            nameof(TertiaryDetailText),
            typeof(string),
            typeof(SettingsInfoItemView),
            string.Empty,
            propertyChanged: OnTertiaryDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty TrailingTextProperty = BindableProperty.Create(
            nameof(TrailingText),
            typeof(string),
            typeof(SettingsInfoItemView),
            string.Empty,
            propertyChanged: OnTrailingChipVisibilityPropertyChanged);

        public static readonly BindableProperty IsTrailingTextVisibleProperty = BindableProperty.Create(
            nameof(IsTrailingTextVisible),
            typeof(bool),
            typeof(SettingsInfoItemView),
            true,
            propertyChanged: OnTrailingChipVisibilityPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(SettingsInfoItemView),
            default(Geometry),
            propertyChanged: OnLeadingIconVisibilityPropertyChanged);

        public static readonly BindableProperty AttentionLeadingIconDataProperty = BindableProperty.Create(
            nameof(AttentionLeadingIconData),
            typeof(Geometry),
            typeof(SettingsInfoItemView),
            default(Geometry),
            propertyChanged: OnLeadingIconVisibilityPropertyChanged);

        public static readonly BindableProperty IsAttentionStateProperty = BindableProperty.Create(
            nameof(IsAttentionState),
            typeof(bool),
            typeof(SettingsInfoItemView),
            false,
            propertyChanged: OnLeadingIconVisibilityPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultLeadingIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty AttentionLeadingIconFrameStyleResourceKeyProperty =
            BindableProperty.Create(
                nameof(AttentionLeadingIconFrameStyleResourceKey),
                typeof(string),
                typeof(SettingsInfoItemView),
                DefaultAttentionLeadingIconFrameStyleResourceKey,
                propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultTextStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TitleTextStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultTitleTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TertiaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingChipStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultTrailingChipStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingTextStyleResourceKey),
            typeof(string),
            typeof(SettingsInfoItemView),
            DefaultTrailingTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty AttentionTrailingTextStyleResourceKeyProperty =
            BindableProperty.Create(
                nameof(AttentionTrailingTextStyleResourceKey),
                typeof(string),
                typeof(SettingsInfoItemView),
                DefaultAttentionTrailingTextStyleResourceKey,
                propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;
        private readonly IconFrame _leadingIcon;
        private readonly Label _primaryDetailText;
        private readonly Label _secondaryDetailText;
        private readonly Label _tertiaryDetailText;
        private readonly VerticalStackLayout _textStack;
        private readonly Label _title;
        private readonly ChipView _trailingChip;
        private bool _hasAppliedLeadingIconVisibility;
        private bool _hasAppliedPrimaryDetailTextVisibility;
        private bool _hasAppliedSecondaryDetailTextVisibility;
        private bool _hasAppliedTertiaryDetailTextVisibility;
        private bool _hasAppliedTrailingChipVisibility;

        public SettingsInfoItemView()
        {
            InputTransparent = true;

            _leadingIcon = new IconFrame();
            _title = new Label();
            _primaryDetailText = new Label();
            _secondaryDetailText = new Label();
            _tertiaryDetailText = new Label();
            _trailingChip = new ChipView();
            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _title,
                    _primaryDetailText,
                    _secondaryDetailText,
                    _tertiaryDetailText,
                },
            };

            Grid.SetColumn(_textStack, 1);
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
                    _textStack,
                    _trailingChip,
                },
            };

            Content = _grid;
            UpdateVisualState(
                animateLeadingIconVisibility: false,
                animatePrimaryDetailTextVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateTrailingChipVisibility: false);
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

        public Geometry? AttentionLeadingIconData
        {
            get => (Geometry?)GetValue(AttentionLeadingIconDataProperty);
            set => SetValue(AttentionLeadingIconDataProperty, value);
        }

        public bool IsAttentionState
        {
            get => (bool)GetValue(IsAttentionStateProperty);
            set => SetValue(IsAttentionStateProperty, value);
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

        public string AttentionLeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(AttentionLeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(AttentionLeadingIconFrameStyleResourceKeyProperty, value);
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

        public string AttentionTrailingTextStyleResourceKey
        {
            get => (string)GetValue(AttentionTrailingTextStyleResourceKeyProperty);
            set => SetValue(AttentionTrailingTextStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsInfoItemView view = (SettingsInfoItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animatePrimaryDetailTextVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateTrailingChipVisibility: false);
        }

        private static void OnLeadingIconVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsInfoItemView view = (SettingsInfoItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: true,
                animatePrimaryDetailTextVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateTrailingChipVisibility: false);
        }

        private static void OnPrimaryDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsInfoItemView view = (SettingsInfoItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animatePrimaryDetailTextVisibility: true,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateTrailingChipVisibility: false);
        }

        private static void OnSecondaryDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsInfoItemView view = (SettingsInfoItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animatePrimaryDetailTextVisibility: false,
                animateSecondaryDetailTextVisibility: true,
                animateTertiaryDetailTextVisibility: false,
                animateTrailingChipVisibility: false);
        }

        private static void OnTertiaryDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsInfoItemView view = (SettingsInfoItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animatePrimaryDetailTextVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: true,
                animateTrailingChipVisibility: false);
        }

        private static void OnTrailingChipVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsInfoItemView view = (SettingsInfoItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animatePrimaryDetailTextVisibility: false,
                animateSecondaryDetailTextVisibility: false,
                animateTertiaryDetailTextVisibility: false,
                animateTrailingChipVisibility: true);
        }

        private void UpdateVisualState(
            bool animateLeadingIconVisibility,
            bool animatePrimaryDetailTextVisibility,
            bool animateSecondaryDetailTextVisibility,
            bool animateTertiaryDetailTextVisibility,
            bool animateTrailingChipVisibility)
        {
            string title = Title ?? string.Empty;
            string primaryDetailText = PrimaryDetailText ?? string.Empty;
            string secondaryDetailText = SecondaryDetailText ?? string.Empty;
            string tertiaryDetailText = TertiaryDetailText ?? string.Empty;
            string trailingText = TrailingText ?? string.Empty;
            string gridStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string leadingIconFrameStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                IsAttentionState ? AttentionLeadingIconFrameStyleResourceKey : LeadingIconFrameStyleResourceKey,
                IsAttentionState
                    ? DefaultAttentionLeadingIconFrameStyleResourceKey
                    : DefaultLeadingIconFrameStyleResourceKey);
            string textStackStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TextStackStyleResourceKey, DefaultTextStackStyleResourceKey);
            string titleTextStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TitleTextStyleResourceKey, DefaultTitleTextStyleResourceKey);
            string primaryDetailTextStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(PrimaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string secondaryDetailTextStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(SecondaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string tertiaryDetailTextStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TertiaryDetailTextStyleResourceKey, DefaultDetailTextStyleResourceKey);
            string trailingChipStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TrailingChipStyleResourceKey, DefaultTrailingChipStyleResourceKey);
            string trailingTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                IsAttentionState ? AttentionTrailingTextStyleResourceKey : TrailingTextStyleResourceKey,
                IsAttentionState
                    ? DefaultAttentionTrailingTextStyleResourceKey
                    : DefaultTrailingTextStyleResourceKey);
            Geometry? leadingIconData = IsAttentionState && AttentionLeadingIconData is not null
                ? AttentionLeadingIconData
                : LeadingIconData;
            bool isLeadingIconVisible = leadingIconData is not null;
            bool isLeadingIconLayoutVisible = ResolveLeadingIconLayoutVisibility(
                isLeadingIconVisible,
                animateLeadingIconVisibility);
            bool isTrailingChipVisible = IsTrailingChipActuallyVisible(trailingText);
            bool isTrailingChipLayoutVisible = ResolveTrailingChipLayoutVisibility(
                isTrailingChipVisible,
                animateTrailingChipVisibility);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            if (isLeadingIconVisible)
            {
                _leadingIcon.IconData = leadingIconData;
            }

            UpdateLeadingIconVisibility(leadingIconData, animateLeadingIconVisibility);
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
            _trailingChip.Text = trailingText;
            _trailingChip.ChipStyleResourceKey = trailingChipStyleResourceKey;
            _trailingChip.LabelStyleResourceKey = trailingTextStyleResourceKey;

            Grid.SetColumn(_textStack, isLeadingIconLayoutVisible ? 1 : 0);
            Grid.SetColumnSpan(
                _textStack,
                ResolveTextColumnSpan(isLeadingIconLayoutVisible, isTrailingChipLayoutVisible));
            UpdateTrailingChipVisibility(trailingText, animateTrailingChipVisibility);
            SemanticProperties.SetDescription(
                this,
                CreateSemanticDescription(
                    title,
                    primaryDetailText,
                    secondaryDetailText,
                    tertiaryDetailText,
                    trailingText));
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

        private void UpdateLeadingIconVisibility(Geometry? leadingIconData, bool animateLeadingIconVisibility)
        {
            bool isLeadingIconVisible = leadingIconData is not null;
            bool shouldAnimate = animateLeadingIconVisibility && _hasAppliedLeadingIconVisibility;
            double targetOpacity = isLeadingIconVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isLeadingIconVisible)
            {
                _leadingIcon.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _leadingIcon,
                _leadingIcon.Opacity,
                targetOpacity,
                duration,
                LeadingIconOpacityAnimationName,
                shouldAnimate,
                opacity => _leadingIcon.Opacity = opacity,
                CompleteLeadingIconVisibility);
            _hasAppliedLeadingIconVisibility = true;
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

        private void CompleteLeadingIconVisibility()
        {
            Geometry? leadingIconData = ResolveLeadingIconData();
            if (leadingIconData is not null)
            {
                _leadingIcon.IconData = leadingIconData;
                _leadingIcon.IsVisible = true;
                return;
            }

            _leadingIcon.IconData = null;
            _leadingIcon.IsVisible = false;
            bool isTrailingChipLayoutVisible =
                IsTrailingChipActuallyVisible(TrailingText ?? string.Empty) || _trailingChip.IsVisible;
            Grid.SetColumn(_textStack, 0);
            Grid.SetColumnSpan(_textStack, ResolveTextColumnSpan(false, isTrailingChipLayoutVisible));
        }

        private void UpdateTrailingChipVisibility(string trailingText, bool animateTrailingChipVisibility)
        {
            bool isTrailingChipVisible = IsTrailingChipActuallyVisible(trailingText);
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
            if (IsTrailingChipActuallyVisible(TrailingText ?? string.Empty))
            {
                _trailingChip.IsVisible = true;
                return;
            }

            _trailingChip.IsVisible = false;
            Grid.SetColumnSpan(
                _textStack,
                ResolveTextColumnSpan(_leadingIcon.IsVisible, isTrailingTextVisible: false));
        }

        private static bool IsDetailTextActuallyVisible(string detailText)
        {
            return !string.IsNullOrWhiteSpace(detailText);
        }

        private bool ResolveTrailingChipLayoutVisibility(
            bool isTrailingChipVisible,
            bool animateTrailingChipVisibility)
        {
            if (isTrailingChipVisible)
            {
                return true;
            }

            return animateTrailingChipVisibility && _hasAppliedTrailingChipVisibility && _trailingChip.IsVisible;
        }

        private bool ResolveLeadingIconLayoutVisibility(
            bool isLeadingIconVisible,
            bool animateLeadingIconVisibility)
        {
            if (isLeadingIconVisible)
            {
                return true;
            }

            return animateLeadingIconVisibility && _hasAppliedLeadingIconVisibility && _leadingIcon.IsVisible;
        }

        private Geometry? ResolveLeadingIconData()
        {
            return IsAttentionState && AttentionLeadingIconData is not null
                ? AttentionLeadingIconData
                : LeadingIconData;
        }

        private bool IsTrailingChipActuallyVisible(string trailingText)
        {
            return IsTrailingTextVisible && !string.IsNullOrWhiteSpace(trailingText);
        }

        private static int ResolveTextColumnSpan(bool isLeadingIconVisible, bool isTrailingTextVisible)
        {
            if (isLeadingIconVisible)
            {
                return isTrailingTextVisible ? 1 : 2;
            }

            return isTrailingTextVisible ? 2 : 3;
        }

        private static string CreateSemanticDescription(
            string title,
            string primaryDetailText,
            string secondaryDetailText,
            string tertiaryDetailText,
            string trailingText)
        {
            List<string> parts = [title, primaryDetailText, secondaryDetailText, tertiaryDetailText, trailingText];
            return string.Join(". ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
    }
}
