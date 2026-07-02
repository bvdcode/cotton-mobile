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
        private const string DefaultSupportingTextStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultSwitchStyleResourceKey = "M3Switch";
        private const string DefaultTextStackStyleResourceKey = "M3SettingsDenseStack";
        private const string DefaultTextStyleResourceKey = "M3LabelLargeLine";

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
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSupportingTextVisibleProperty = BindableProperty.Create(
            nameof(IsSupportingTextVisible),
            typeof(bool),
            typeof(SettingsToggleItemView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(SettingsToggleItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsDetailTextVisibleProperty = BindableProperty.Create(
            nameof(IsDetailTextVisible),
            typeof(bool),
            typeof(SettingsToggleItemView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(SettingsToggleItemView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLeadingIconVisibleProperty = BindableProperty.Create(
            nameof(IsLeadingIconVisible),
            typeof(bool),
            typeof(SettingsToggleItemView),
            true,
            propertyChanged: OnVisualPropertyChanged);

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
            UpdateVisualState();
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
                UpdateVisualState();
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsToggleItemView view = (SettingsToggleItemView)bindable;
            view.UpdateVisualState();
        }

        private bool CanToggle()
        {
            return IsEnabled;
        }

        private void Toggle()
        {
            IsToggled = !IsToggled;
        }

        private void UpdateVisualState()
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
            string gridStyleResourceKey = ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyleResourceKey);
            string textStyleResourceKey = ResolveStyleResourceKey(TextStyleResourceKey, DefaultTextStyleResourceKey);
            string supportingTextStyleResourceKey = ResolveStyleResourceKey(
                SupportingTextStyleResourceKey,
                DefaultSupportingTextStyleResourceKey);
            string detailTextStyleResourceKey = ResolveStyleResourceKey(
                DetailTextStyleResourceKey,
                DefaultDetailTextStyleResourceKey);
            string textStackStyleResourceKey = ResolveStyleResourceKey(
                TextStackStyleResourceKey,
                DefaultTextStackStyleResourceKey);
            string leadingIconFrameStyleResourceKey = ResolveStyleResourceKey(
                LeadingIconFrameStyleResourceKey,
                DefaultLeadingIconFrameStyleResourceKey);
            string switchStyleResourceKey = ResolveStyleResourceKey(SwitchStyleResourceKey, DefaultSwitchStyleResourceKey);
            bool isLeadingIconVisible = IsLeadingIconVisible && LeadingIconData is not null;

            _container.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _text.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _supportingText.SetDynamicResource(StyleProperty, supportingTextStyleResourceKey);
            _detailText.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _toggleSwitch.SetDynamicResource(StyleProperty, switchStyleResourceKey);

            _leadingIcon.IconData = LeadingIconData;
            _leadingIcon.IsVisible = isLeadingIconVisible;
            _text.Text = text;
            _supportingText.Text = supportingText;
            _supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
            _detailText.Text = detailText;
            _detailText.IsVisible = IsDetailTextVisible && !string.IsNullOrWhiteSpace(detailText);
            _toggleSwitch.IsEnabled = IsEnabled;
            _touchSurface.TapCommand = IsEnabled ? _toggleCommand : null;
            _touchSurface.IsVisible = IsEnabled;

            Grid.SetColumn(_textStack, isLeadingIconVisible ? 1 : 0);
            Grid.SetColumnSpan(_textStack, isLeadingIconVisible ? 1 : 2);

            SemanticProperties.SetDescription(_container, semanticDescription);
            SemanticProperties.SetDescription(_toggleSwitch, toggleSemanticDescription);
        }

        private static string ResolveStyleResourceKey(string resourceKey, string defaultResourceKey)
        {
            return string.IsNullOrWhiteSpace(resourceKey)
                ? defaultResourceKey
                : resourceKey;
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
