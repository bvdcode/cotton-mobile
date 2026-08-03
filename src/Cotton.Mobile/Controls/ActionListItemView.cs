// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class ActionListItemView : ContentView
    {
        private const string DefaultActionIconButtonStyle = "M3DefaultIconButton";
        private const string DefaultGridStyle = "M3ActionListItemGrid";
        private const string DefaultLeadingIconStyle = "M3ActionListItemIconFrame";
        private const string DefaultTextStyle = "M3ActionListItemLabel";
        private const string SupportingTextStyle = "M3CardSupportingBlock";
        private const string TextStackStyle = "M3CardTextStack";

        public static readonly BindableProperty TextProperty = CreateTextProperty(nameof(Text));
        public static readonly BindableProperty SupportingTextProperty = CreateTextProperty(nameof(SupportingText));
        public static readonly BindableProperty SemanticDescriptionProperty =
            CreateTextProperty(nameof(SemanticDescription));

        public static readonly BindableProperty LeadingIconDataProperty = CreateGeometryProperty(nameof(LeadingIconData));
        public static readonly BindableProperty ActionIconDataProperty = CreateGeometryProperty(nameof(ActionIconData));

        public static readonly BindableProperty IsLeadingIconVisibleProperty = BindableProperty.Create(
            nameof(IsLeadingIconVisible),
            typeof(bool),
            typeof(ActionListItemView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(ActionListItemView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(ActionListItemView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(ActionListItemView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty =
            CreateStyleProperty(nameof(GridStyleResourceKey), DefaultGridStyle);
        public static readonly BindableProperty TextStyleResourceKeyProperty =
            CreateStyleProperty(nameof(TextStyleResourceKey), DefaultTextStyle);
        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty =
            CreateStyleProperty(nameof(LeadingIconFrameStyleResourceKey), DefaultLeadingIconStyle);
        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty =
            CreateStyleProperty(nameof(ActionIconButtonStyleResourceKey), DefaultActionIconButtonStyle);

        private readonly IconButton _actionButton;
        private readonly Grid _container;
        private readonly IconFrame _leadingIcon;
        private readonly Label _supportingText;
        private readonly TapGestureRecognizer _tapGesture;
        private readonly Label _text;
        private readonly VerticalStackLayout _textStack;

        public ActionListItemView()
        {
            _leadingIcon = new IconFrame();
            _text = new Label();
            _supportingText = new Label();
            _supportingText.SetDynamicResource(StyleProperty, SupportingTextStyle);
            _textStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    _text,
                    _supportingText,
                },
            };
            _textStack.SetDynamicResource(StyleProperty, TextStackStyle);

            _actionButton = new IconButton();
            _container = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                Children =
                {
                    _leadingIcon,
                    _textStack,
                    _actionButton,
                },
            };
            Grid.SetColumn(_textStack, 1);
            Grid.SetColumn(_actionButton, 2);

            _tapGesture = new TapGestureRecognizer();
            _container.GestureRecognizers.Add(_tapGesture);

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

        public Geometry? ActionIconData
        {
            get => (Geometry?)GetValue(ActionIconDataProperty);
            set => SetValue(ActionIconDataProperty, value);
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public string SemanticDescription
        {
            get => (string)GetValue(SemanticDescriptionProperty);
            set => SetValue(SemanticDescriptionProperty, value);
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

        public string LeadingIconFrameStyleResourceKey
        {
            get => (string)GetValue(LeadingIconFrameStyleResourceKeyProperty);
            set => SetValue(LeadingIconFrameStyleResourceKeyProperty, value);
        }

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        private static BindableProperty CreateTextProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(ActionListItemView),
                string.Empty,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static BindableProperty CreateGeometryProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(Geometry),
                typeof(ActionListItemView),
                default(Geometry),
                propertyChanged: OnVisualPropertyChanged);
        }

        private static BindableProperty CreateStyleProperty(string name, string defaultValue)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(ActionListItemView),
                defaultValue,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((ActionListItemView)bindable).UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_container is null)
            {
                return;
            }

            string text = Text ?? string.Empty;
            string supportingText = SupportingText ?? string.Empty;
            bool hasLeadingIcon = IsLeadingIconVisible && LeadingIconData is not null;
            bool hasAction = ActionIconData is not null;
            string semanticDescription = string.IsNullOrWhiteSpace(SemanticDescription)
                ? string.Join(", ", new[] { text, supportingText }
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
                : SemanticDescription;

            _container.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(GridStyleResourceKey, DefaultGridStyle));
            _text.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(TextStyleResourceKey, DefaultTextStyle));
            _leadingIcon.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(
                    LeadingIconFrameStyleResourceKey,
                    DefaultLeadingIconStyle));
            _actionButton.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(
                    ActionIconButtonStyleResourceKey,
                    DefaultActionIconButtonStyle));

            _text.Text = text;
            _supportingText.Text = supportingText;
            _supportingText.IsVisible = !string.IsNullOrWhiteSpace(supportingText);

            _leadingIcon.IconData = LeadingIconData;
            _leadingIcon.IsVisible = hasLeadingIcon;

            _actionButton.IconData = ActionIconData;
            _actionButton.Command = Command;
            _actionButton.CommandParameter = CommandParameter;
            _actionButton.IsEnabled = IsActionEnabled;
            _actionButton.IsVisible = hasAction;

            int textColumn = hasLeadingIcon ? 1 : 0;
            Grid.SetColumn(_textStack, textColumn);
            Grid.SetColumnSpan(_textStack, 3 - textColumn - (hasAction ? 1 : 0));

            _tapGesture.Command = IsActionEnabled ? Command : null;
            _tapGesture.CommandParameter = CommandParameter;

            SemanticProperties.SetDescription(_container, semanticDescription);
            SemanticProperties.SetDescription(_actionButton, semanticDescription);
        }
    }
}
