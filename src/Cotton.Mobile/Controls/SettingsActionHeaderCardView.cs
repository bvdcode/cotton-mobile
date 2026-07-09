// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class SettingsActionHeaderCardView : ContentView
    {
        private const string DefaultActionClusterStyleResourceKey = "M3InlineActionCluster";
        private const string DefaultCardStyleResourceKey = "M3ContentCard";
        private const string DefaultPrimaryDetailTextStyleResourceKey = "M3CardSupportingLine";
        private const string DefaultSecondaryActionIconButtonStyleResourceKey = "M3DefaultIconButton";

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(SettingsActionHeaderCardView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextProperty = BindableProperty.Create(
            nameof(PrimaryDetailText),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextProperty = BindableProperty.Create(
            nameof(SecondaryDetailText),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextProperty = BindableProperty.Create(
            nameof(TertiaryDetailText),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty QuaternaryDetailTextProperty = BindableProperty.Create(
            nameof(QuaternaryDetailText),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            DefaultPrimaryDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TertiaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty QuaternaryDetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(QuaternaryDetailTextStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CardStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CardStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            DefaultCardStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionClusterStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionClusterStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            DefaultActionClusterStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(SettingsActionHeaderCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TapCommandParameterProperty = BindableProperty.Create(
            nameof(TapCommandParameter),
            typeof(object),
            typeof(SettingsActionHeaderCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsTapEnabledProperty = BindableProperty.Create(
            nameof(IsTapEnabled),
            typeof(bool),
            typeof(SettingsActionHeaderCardView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionIconDataProperty = BindableProperty.Create(
            nameof(PrimaryActionIconData),
            typeof(Geometry),
            typeof(SettingsActionHeaderCardView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionCommandProperty = BindableProperty.Create(
            nameof(PrimaryActionCommand),
            typeof(ICommand),
            typeof(SettingsActionHeaderCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(PrimaryActionSemanticDescription),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconDataProperty = BindableProperty.Create(
            nameof(SecondaryActionIconData),
            typeof(Geometry),
            typeof(SettingsActionHeaderCardView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionCommandProperty = BindableProperty.Create(
            nameof(SecondaryActionCommand),
            typeof(ICommand),
            typeof(SettingsActionHeaderCardView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            DefaultSecondaryActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(SecondaryActionSemanticDescription),
            typeof(string),
            typeof(SettingsActionHeaderCardView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ActionClusterView _actions;
        private readonly ContentCardView _card;
        private readonly SettingsSectionHeaderView _header;

        public SettingsActionHeaderCardView()
        {
            _actions = new ActionClusterView();
            _header = new SettingsSectionHeaderView
            {
                TrailingContent = _actions,
            };
            _card = new ContentCardView
            {
                BodyContent = _header,
            };

            Content = _card;
            UpdateVisualState();
        }

        public Geometry? LeadingIconData
        {
            get => (Geometry?)GetValue(LeadingIconDataProperty);
            set => SetValue(LeadingIconDataProperty, value);
        }

        public string LeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(LeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(LeadingIconFrameStyleResourceKeyProperty, value);
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

        public string CardStyleResourceKey
        {
            get => (string)GetValue(CardStyleResourceKeyProperty);
            set => SetValue(CardStyleResourceKeyProperty, value);
        }

        public string ActionClusterStyleResourceKey
        {
            get => (string)GetValue(ActionClusterStyleResourceKeyProperty);
            set => SetValue(ActionClusterStyleResourceKeyProperty, value);
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

        public Geometry? PrimaryActionIconData
        {
            get => (Geometry?)GetValue(PrimaryActionIconDataProperty);
            set => SetValue(PrimaryActionIconDataProperty, value);
        }

        public ICommand? PrimaryActionCommand
        {
            get => (ICommand?)GetValue(PrimaryActionCommandProperty);
            set => SetValue(PrimaryActionCommandProperty, value);
        }

        public string PrimaryActionSemanticDescription
        {
            get => (string)GetValue(PrimaryActionSemanticDescriptionProperty);
            set => SetValue(PrimaryActionSemanticDescriptionProperty, value);
        }

        public Geometry? SecondaryActionIconData
        {
            get => (Geometry?)GetValue(SecondaryActionIconDataProperty);
            set => SetValue(SecondaryActionIconDataProperty, value);
        }

        public ICommand? SecondaryActionCommand
        {
            get => (ICommand?)GetValue(SecondaryActionCommandProperty);
            set => SetValue(SecondaryActionCommandProperty, value);
        }

        public string SecondaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(SecondaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(SecondaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public string SecondaryActionSemanticDescription
        {
            get => (string)GetValue(SecondaryActionSemanticDescriptionProperty);
            set => SetValue(SecondaryActionSemanticDescriptionProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsActionHeaderCardView view = (SettingsActionHeaderCardView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string cardStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                CardStyleResourceKey,
                DefaultCardStyleResourceKey);
            string actionClusterStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ActionClusterStyleResourceKey,
                DefaultActionClusterStyleResourceKey);
            string primaryDetailTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                PrimaryDetailTextStyleResourceKey,
                DefaultPrimaryDetailTextStyleResourceKey);
            string secondaryActionIconButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                SecondaryActionIconButtonStyleResourceKey,
                DefaultSecondaryActionIconButtonStyleResourceKey);

            _card.CardStyleResourceKey = cardStyleResourceKey;

            _header.LeadingIconData = LeadingIconData;
            _header.LeadingIconFrameStyleResourceKey = LeadingIconFrameStyleResourceKey;
            _header.Title = Title ?? string.Empty;
            _header.PrimaryDetailText = PrimaryDetailText ?? string.Empty;
            _header.PrimaryDetailTextStyleResourceKey = primaryDetailTextStyleResourceKey;
            _header.SecondaryDetailText = SecondaryDetailText ?? string.Empty;
            _header.SecondaryDetailTextStyleResourceKey = SecondaryDetailTextStyleResourceKey;
            _header.TertiaryDetailText = TertiaryDetailText ?? string.Empty;
            _header.TertiaryDetailTextStyleResourceKey = TertiaryDetailTextStyleResourceKey;
            _header.QuaternaryDetailText = QuaternaryDetailText ?? string.Empty;
            _header.QuaternaryDetailTextStyleResourceKey = QuaternaryDetailTextStyleResourceKey;
            _header.TapCommand = TapCommand;
            _header.TapCommandParameter = TapCommandParameter;
            _header.IsTapEnabled = IsTapEnabled;

            _actions.ClusterStyleResourceKey = actionClusterStyleResourceKey;
            _actions.PrimaryActionIconData = PrimaryActionIconData;
            _actions.PrimaryActionCommand = PrimaryActionCommand;
            _actions.PrimaryActionSemanticDescription = PrimaryActionSemanticDescription ?? string.Empty;
            _actions.SecondaryActionIconData = SecondaryActionIconData;
            _actions.SecondaryActionCommand = SecondaryActionCommand;
            _actions.SecondaryActionIconButtonStyleResourceKey = secondaryActionIconButtonStyleResourceKey;
            _actions.SecondaryActionSemanticDescription = SecondaryActionSemanticDescription ?? string.Empty;
        }
    }
}
