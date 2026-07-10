// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Collections.Generic;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SettingsToggleItemView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3SettingsListItemGrid";
        private const string DefaultDetailTextStyleResourceKey = "M3CardSupportingBlock";
        private const string DefaultLeadingIconFrameStyleResourceKey = "M3CardUtilityThumbnailFrame";
        private const string DefaultSupportingTextStyleResourceKey = "M3CardSupportingWrap";
        private const string DefaultSwitchStyleResourceKey = "M3Switch";
        private const string DefaultTextStackStyleResourceKey = "M3SettingsDenseStack";
        private const string DefaultTextStyleResourceKey = "M3LabelLargeLine";
        private const string DetailTextOpacityAnimationName = "M3SettingsToggleDetailTextOpacity";
        private const string LeadingIconOpacityAnimationName = "M3SettingsToggleLeadingIconOpacity";
        private const string SupportingTextOpacityAnimationName = "M3SettingsToggleSupportingTextOpacity";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(SettingsToggleItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
            nameof(SupportingText),
            typeof(string),
            typeof(SettingsToggleItemView),
            string.Empty,
            propertyChanged: OnSupportingTextVisibilityPropertyChanged);

        public static readonly BindableProperty IsSupportingTextVisibleProperty = BindableProperty.Create(
            nameof(IsSupportingTextVisible),
            typeof(bool),
            typeof(SettingsToggleItemView),
            true,
            propertyChanged: OnSupportingTextVisibilityPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(SettingsToggleItemView),
            string.Empty,
            propertyChanged: OnDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty IsDetailTextVisibleProperty = BindableProperty.Create(
            nameof(IsDetailTextVisible),
            typeof(bool),
            typeof(SettingsToggleItemView),
            true,
            propertyChanged: OnDetailTextVisibilityPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(SettingsToggleItemView),
            default(Geometry),
            propertyChanged: OnLeadingIconVisibilityPropertyChanged);

        public static readonly BindableProperty IsLeadingIconVisibleProperty = BindableProperty.Create(
            nameof(IsLeadingIconVisible),
            typeof(bool),
            typeof(SettingsToggleItemView),
            true,
            propertyChanged: OnLeadingIconVisibilityPropertyChanged);

        public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
            nameof(IsToggled),
            typeof(bool),
            typeof(SettingsToggleItemView),
            false,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(SettingsToggleItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ToggleSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ToggleSemanticDescription),
            typeof(string),
            typeof(SettingsToggleItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(SettingsToggleItemView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStyleResourceKey),
            typeof(string),
            typeof(SettingsToggleItemView),
            DefaultTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SupportingTextStyleResourceKey),
            typeof(string),
            typeof(SettingsToggleItemView),
            DefaultSupportingTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsToggleItemView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(SettingsToggleItemView),
            DefaultTextStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(SettingsToggleItemView),
            DefaultLeadingIconFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SwitchStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SwitchStyleResourceKey),
            typeof(string),
            typeof(SettingsToggleItemView),
            DefaultSwitchStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _container;
        private readonly Label _detailText;
        private readonly IconFrame _leadingIcon;
        private readonly Label _supportingText;
        private readonly Label _text;
        private readonly VerticalStackLayout _textStack;
        private readonly TouchSurfaceView _touchSurface;
        private readonly Command _toggleCommand;
        private readonly ToggleSwitch _toggleSwitch;
        private bool _hasAppliedDetailTextVisibility;
        private bool _hasAppliedLeadingIconVisibility;
        private bool _hasAppliedSupportingTextVisibility;

        public SettingsToggleItemView()
        {
            _toggleCommand = new Command(Toggle, CanToggle);
            _leadingIcon = new IconFrame();

            _text = new Label();
            _supportingText = new Label();
            _detailText = new Label();
            _textStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    _text,
                    _supportingText,
                    _detailText,
                },
            };

            _touchSurface = new TouchSurfaceView();
            Grid.SetColumnSpan(_touchSurface, 2);

            _toggleSwitch = new ToggleSwitch();
            _toggleSwitch.SetBinding(
                ToggleSwitch.IsToggledProperty,
                new Binding(nameof(IsToggled), source: this, mode: BindingMode.TwoWay));
            Grid.SetColumn(_toggleSwitch, 2);

            _container = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Star,
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                },
                Children =
                {
                    _leadingIcon,
                    _textStack,
                    _touchSurface,
                    _toggleSwitch,
                },
            };

            Content = _container;
            UpdateVisualState(
                animateLeadingIconVisibility: false,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: false);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string SupportingText
        {
            get => (string)GetValue(SupportingTextProperty);
            set => SetValue(SupportingTextProperty, value);
        }

        public bool IsSupportingTextVisible
        {
            get => (bool)GetValue(IsSupportingTextVisibleProperty);
            set => SetValue(IsSupportingTextVisibleProperty, value);
        }

        public string DetailText
        {
            get => (string)GetValue(DetailTextProperty);
            set => SetValue(DetailTextProperty, value);
        }

        public bool IsDetailTextVisible
        {
            get => (bool)GetValue(IsDetailTextVisibleProperty);
            set => SetValue(IsDetailTextVisibleProperty, value);
        }

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public bool IsLeadingIconVisible
        {
            get => (bool)GetValue(IsLeadingIconVisibleProperty);
            set => SetValue(IsLeadingIconVisibleProperty, value);
        }

        public bool IsToggled
        {
            get => (bool)GetValue(IsToggledProperty);
            set => SetValue(IsToggledProperty, value);
        }

        public string SemanticDescription
        {
            get => (string)GetValue(SemanticDescriptionProperty);
            set => SetValue(SemanticDescriptionProperty, value);
        }

        public string ToggleSemanticDescription
        {
            get => (string)GetValue(ToggleSemanticDescriptionProperty);
            set => SetValue(ToggleSemanticDescriptionProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string TextStyleResourceKey
        {
            get => (string)GetValue(TextStyleResourceKeyProperty);
            set => SetValue(TextStyleResourceKeyProperty, value);
        }

        public string SupportingTextStyleResourceKey
        {
            get => (string)GetValue(SupportingTextStyleResourceKeyProperty);
            set => SetValue(SupportingTextStyleResourceKeyProperty, value);
        }

        public string DetailTextStyleResourceKey
        {
            get => (string)GetValue(DetailTextStyleResourceKeyProperty);
            set => SetValue(DetailTextStyleResourceKeyProperty, value);
        }

        public string TextStackStyleResourceKey
        {
            get => (string)GetValue(TextStackStyleResourceKeyProperty);
            set => SetValue(TextStackStyleResourceKeyProperty, value);
        }

        public string LeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(LeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(LeadingIconFrameStyleResourceKeyProperty, value);
        }

        public string SwitchStyleResourceKey
        {
            get => (string)GetValue(SwitchStyleResourceKeyProperty);
            set => SetValue(SwitchStyleResourceKeyProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal))
            {
                _toggleCommand.ChangeCanExecute();
                UpdateVisualState(
                    animateLeadingIconVisibility: false,
                    animateSupportingTextVisibility: false,
                    animateDetailTextVisibility: false);
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsToggleItemView view = (SettingsToggleItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: false);
        }

        private static void OnLeadingIconVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsToggleItemView view = (SettingsToggleItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: true,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: false);
        }

        private static void OnSupportingTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsToggleItemView view = (SettingsToggleItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animateSupportingTextVisibility: true,
                animateDetailTextVisibility: false);
        }

        private static void OnDetailTextVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            SettingsToggleItemView view = (SettingsToggleItemView)bindable;
            view.UpdateVisualState(
                animateLeadingIconVisibility: false,
                animateSupportingTextVisibility: false,
                animateDetailTextVisibility: true);
        }

        private bool CanToggle()
        {
            return IsEnabled;
        }

        private void Toggle()
        {
            IsToggled = !IsToggled;
        }

        private void UpdateVisualState(
            bool animateLeadingIconVisibility,
            bool animateSupportingTextVisibility,
            bool animateDetailTextVisibility)
        {
            string text = Text ?? string.Empty;
            string supportingText = SupportingText ?? string.Empty;
            string detailText = DetailText ?? string.Empty;
            string semanticDescription = string.IsNullOrWhiteSpace(SemanticDescription)
                ? CreateSemanticDescription(text, supportingText, detailText)
                : SemanticDescription;
            string toggleSemanticDescription = string.IsNullOrWhiteSpace(ToggleSemanticDescription)
                ? semanticDescription
                : ToggleSemanticDescription;
            string gridStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string textStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(TextStyleResourceKey, DefaultTextStyleResourceKey);
            string supportingTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                SupportingTextStyleResourceKey,
                DefaultSupportingTextStyleResourceKey);
            string detailTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                DetailTextStyleResourceKey,
                DefaultDetailTextStyleResourceKey);
            string textStackStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                TextStackStyleResourceKey,
                DefaultTextStackStyleResourceKey);
            string leadingIconFrameStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                LeadingIconFrameStyleResourceKey,
                DefaultLeadingIconFrameStyleResourceKey);
            string switchStyleResourceKey =
                MaterialResources.ResolveStyleResourceKey(SwitchStyleResourceKey, DefaultSwitchStyleResourceKey);
            bool isLeadingIconVisible = IsLeadingIconVisible && LeadingIconData is not null;
            bool hasLeadingIconLayout = ResolveLeadingIconLayoutVisibility(
                isLeadingIconVisible,
                animateLeadingIconVisibility);

            _container.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _text.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _supportingText.SetDynamicResource(StyleProperty, supportingTextStyleResourceKey);
            _detailText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _toggleSwitch.SetDynamicResource(StyleProperty, switchStyleResourceKey);

            if (isLeadingIconVisible)
            {
                _leadingIcon.IconData = LeadingIconData;
            }

            UpdateLeadingIconVisibility(isLeadingIconVisible, animateLeadingIconVisibility);
            _text.Text = text;
            _supportingText.Text = supportingText;
            UpdateSupportingTextVisibility(supportingText, animateSupportingTextVisibility);
            _detailText.Text = detailText;
            UpdateDetailTextVisibility(detailText, animateDetailTextVisibility);
            _toggleSwitch.IsEnabled = IsEnabled;
            _touchSurface.TapCommand = IsEnabled ? _toggleCommand : null;
            _touchSurface.IsVisible = IsEnabled;

            Grid.SetColumn(_textStack, hasLeadingIconLayout ? 1 : 0);
            Grid.SetColumnSpan(_textStack, hasLeadingIconLayout ? 1 : 2);

            SemanticProperties.SetDescription(_container, semanticDescription);
            SemanticProperties.SetDescription(_toggleSwitch, toggleSemanticDescription);
        }

        private void UpdateLeadingIconVisibility(bool isLeadingIconVisible, bool animateLeadingIconVisibility)
        {
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

        private void UpdateSupportingTextVisibility(string supportingText, bool animateSupportingTextVisibility)
        {
            bool isSupportingTextVisible = IsSupportingTextActuallyVisible(supportingText);
            bool shouldAnimate = animateSupportingTextVisibility && _hasAppliedSupportingTextVisibility;
            double targetOpacity = isSupportingTextVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isSupportingTextVisible)
            {
                _supportingText.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _supportingText,
                _supportingText.Opacity,
                targetOpacity,
                duration,
                SupportingTextOpacityAnimationName,
                shouldAnimate,
                opacity => _supportingText.Opacity = opacity,
                CompleteSupportingTextVisibility);
            _hasAppliedSupportingTextVisibility = true;
        }

        private void UpdateDetailTextVisibility(string detailText, bool animateDetailTextVisibility)
        {
            bool isDetailTextVisible = IsDetailTextActuallyVisible(detailText);
            bool shouldAnimate = animateDetailTextVisibility && _hasAppliedDetailTextVisibility;
            double targetOpacity = isDetailTextVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isDetailTextVisible)
            {
                _detailText.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _detailText,
                _detailText.Opacity,
                targetOpacity,
                duration,
                DetailTextOpacityAnimationName,
                shouldAnimate,
                opacity => _detailText.Opacity = opacity,
                CompleteDetailTextVisibility);
            _hasAppliedDetailTextVisibility = true;
        }

        private void CompleteSupportingTextVisibility()
        {
            if (IsSupportingTextActuallyVisible(SupportingText ?? string.Empty))
            {
                _supportingText.IsVisible = true;
                return;
            }

            _supportingText.IsVisible = false;
        }

        private void CompleteDetailTextVisibility()
        {
            if (IsDetailTextActuallyVisible(DetailText ?? string.Empty))
            {
                _detailText.IsVisible = true;
                return;
            }

            _detailText.IsVisible = false;
        }

        private void CompleteLeadingIconVisibility()
        {
            if (IsLeadingIconActuallyVisible())
            {
                _leadingIcon.IconData = LeadingIconData;
                _leadingIcon.IsVisible = true;
                return;
            }

            _leadingIcon.IconData = null;
            _leadingIcon.IsVisible = false;
            Grid.SetColumn(_textStack, 0);
            Grid.SetColumnSpan(_textStack, 2);
        }

        private bool IsSupportingTextActuallyVisible(string supportingText)
        {
            return IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
        }

        private bool IsDetailTextActuallyVisible(string detailText)
        {
            return IsDetailTextVisible && !string.IsNullOrWhiteSpace(detailText);
        }

        private bool IsLeadingIconActuallyVisible()
        {
            return IsLeadingIconVisible && LeadingIconData is not null;
        }

        private bool ResolveLeadingIconLayoutVisibility(bool isLeadingIconVisible, bool animateLeadingIconVisibility)
        {
            if (isLeadingIconVisible)
            {
                return true;
            }

            return animateLeadingIconVisibility && _hasAppliedLeadingIconVisibility && _leadingIcon.IsVisible;
        }

        private static string CreateSemanticDescription(string text, string supportingText, string detailText)
        {
            List<string> semanticParts = [];
            if (!string.IsNullOrWhiteSpace(text))
            {
                semanticParts.Add(text);
            }

            if (!string.IsNullOrWhiteSpace(supportingText))
            {
                semanticParts.Add(supportingText);
            }

            if (!string.IsNullOrWhiteSpace(detailText))
            {
                semanticParts.Add(detailText);
            }

            return string.Join(". ", semanticParts);
        }
    }
}
