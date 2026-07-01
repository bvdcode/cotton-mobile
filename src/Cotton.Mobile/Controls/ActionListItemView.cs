// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Behaviors;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class ActionListItemView : ContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ActionListItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
            nameof(SupportingText),
            typeof(string),
            typeof(ActionListItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSupportingTextVisibleProperty = BindableProperty.Create(
            nameof(IsSupportingTextVisible),
            typeof(bool),
            typeof(ActionListItemView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconDataProperty = BindableProperty.Create(
            nameof(LeadingIconData),
            typeof(Geometry),
            typeof(ActionListItemView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLeadingIconVisibleProperty = BindableProperty.Create(
            nameof(IsLeadingIconVisible),
            typeof(bool),
            typeof(ActionListItemView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconDataProperty = BindableProperty.Create(
            nameof(ActionIconData),
            typeof(Geometry),
            typeof(ActionListItemView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(ActionListItemView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(ActionListItemView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsRowTapEnabledProperty = BindableProperty.Create(
            nameof(IsRowTapEnabled),
            typeof(bool),
            typeof(ActionListItemView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(ActionListItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3ActionListItemGrid",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3ActionListItemLabel",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SupportingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SupportingTextStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3CardSupportingBlock",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3CardTextStack",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LeadingIconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LeadingIconFrameStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3ActivityThumbnailFrame",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3FileChromeIconButton",
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private readonly Grid _container;
        private readonly IconFrame _leadingIcon;
        private readonly Label _supportingText;
        private readonly Label _text;
        private readonly VerticalStackLayout _textStack;
        private readonly LongPressBehavior _tapBehavior;
        private readonly Grid _touchSurface;

        public ActionListItemView()
        {
            _leadingIcon = new IconFrame();

            _text = new Label();
            _supportingText = new Label();

            _textStack = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    _text,
                    _supportingText,
                },
            };

            _tapBehavior = new LongPressBehavior();

            _touchSurface = new Grid();
            _touchSurface.SetDynamicResource(StyleProperty, "M3ListItemTouchSurface");
            _touchSurface.Behaviors.Add(_tapBehavior);
            Grid.SetColumnSpan(_touchSurface, 3);

            _actionButton = new IconButton();
            Grid.SetColumn(_actionButton, 2);

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
                    _actionButton,
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

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public bool IsRowTapEnabled
        {
            get => (bool)GetValue(IsRowTapEnabledProperty);
            set => SetValue(IsRowTapEnabledProperty, value);
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

        public string SupportingTextStyleResourceKey
        {
            get => (string)GetValue(SupportingTextStyleResourceKeyProperty);
            set => SetValue(SupportingTextStyleResourceKeyProperty, value);
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

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ActionListItemView actionListItemView = (ActionListItemView)bindable;
            actionListItemView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string text = Text ?? string.Empty;
            string supportingText = SupportingText ?? string.Empty;
            string semanticDescription = string.IsNullOrWhiteSpace(SemanticDescription)
                ? CreateSemanticDescription(text, supportingText)
                : SemanticDescription;
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? "M3ActionListItemGrid"
                : GridStyleResourceKey;
            string textStyleResourceKey = string.IsNullOrWhiteSpace(TextStyleResourceKey)
                ? "M3ActionListItemLabel"
                : TextStyleResourceKey;
            string supportingTextStyleResourceKey = string.IsNullOrWhiteSpace(SupportingTextStyleResourceKey)
                ? "M3CardSupportingBlock"
                : SupportingTextStyleResourceKey;
            string textStackStyleResourceKey = string.IsNullOrWhiteSpace(TextStackStyleResourceKey)
                ? "M3CardTextStack"
                : TextStackStyleResourceKey;
            string leadingIconFrameStyleResourceKey = string.IsNullOrWhiteSpace(LeadingIconFrameStyleResourceKey)
                ? "M3ActivityThumbnailFrame"
                : LeadingIconFrameStyleResourceKey;
            string actionIconButtonStyleResourceKey = string.IsNullOrWhiteSpace(ActionIconButtonStyleResourceKey)
                ? "M3FileChromeIconButton"
                : ActionIconButtonStyleResourceKey;
            bool isLeadingIconVisible = IsLeadingIconVisible && LeadingIconData is not null;
            ICommand? command = Command;

            _container.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _text.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _supportingText.SetDynamicResource(StyleProperty, supportingTextStyleResourceKey);
            _actionButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);

            _leadingIcon.IconData = LeadingIconData;
            _leadingIcon.IsVisible = isLeadingIconVisible;
            _text.Text = text;
            _supportingText.Text = supportingText;
            _supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
            Grid.SetColumn(_textStack, isLeadingIconVisible ? 1 : 0);
            Grid.SetColumnSpan(_textStack, isLeadingIconVisible ? 1 : 2);

            _actionButton.IconData = ActionIconData;
            _actionButton.Command = command;
            _actionButton.IsEnabled = IsActionEnabled;
            SemanticProperties.SetDescription(_actionButton, semanticDescription);

            _tapBehavior.TapCommand = IsActionEnabled ? command : null;
            _touchSurface.IsVisible = IsRowTapEnabled && IsActionEnabled;
            SemanticProperties.SetDescription(_container, semanticDescription);
        }

        private string CreateSemanticDescription(string text, string supportingText)
        {
            if (string.IsNullOrWhiteSpace(supportingText))
            {
                return text;
            }

            return $"{text}. {supportingText}";
        }
    }
}
