// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Collections.Generic;
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

        public static readonly BindableProperty TrailingTextProperty = BindableProperty.Create(
            nameof(TrailingText),
            typeof(string),
            typeof(ActionListItemView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsTrailingTextVisibleProperty = BindableProperty.Create(
            nameof(IsTrailingTextVisible),
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

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(ActionListItemView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty RowTapCommandProperty = BindableProperty.Create(
            nameof(RowTapCommand),
            typeof(ICommand),
            typeof(ActionListItemView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty RowTapCommandParameterProperty = BindableProperty.Create(
            nameof(RowTapCommandParameter),
            typeof(object),
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

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
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

        public static readonly BindableProperty TrailingChipStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingChipStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3RowActionChip",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TrailingTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TrailingTextStyleResourceKey),
            typeof(string),
            typeof(ActionListItemView),
            "M3ChipLabel",
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
        private readonly Border _trailingChip;
        private readonly Label _trailingText;

        public ActionListItemView()
        {
            _leadingIcon = new IconFrame();

            _text = new Label();
            _supportingText = new Label();
            _trailingText = new Label();

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
            Grid.SetColumnSpan(_touchSurface, 4);

            _trailingChip = new Border
            {
                Content = _trailingText,
            };
            Grid.SetColumn(_trailingChip, 2);

            _actionButton = new IconButton();
            Grid.SetColumn(_actionButton, 3);

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
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto,
                    },
                },
                Children =
                {
                    _leadingIcon,
                    _textStack,
                    _trailingChip,
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

        public ICommand? RowTapCommand
        {
            get => (ICommand?)GetValue(RowTapCommandProperty);
            set => SetValue(RowTapCommandProperty, value);
        }

        public object? RowTapCommandParameter
        {
            get => GetValue(RowTapCommandParameterProperty);
            set => SetValue(RowTapCommandParameterProperty, value);
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

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
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
            string trailingText = TrailingText ?? string.Empty;
            string semanticDescription = string.IsNullOrWhiteSpace(SemanticDescription)
                ? CreateSemanticDescription(text, supportingText, trailingText)
                : SemanticDescription;
            string actionSemanticDescription = string.IsNullOrWhiteSpace(ActionSemanticDescription)
                ? semanticDescription
                : ActionSemanticDescription;
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
            string trailingChipStyleResourceKey = string.IsNullOrWhiteSpace(TrailingChipStyleResourceKey)
                ? "M3RowActionChip"
                : TrailingChipStyleResourceKey;
            string trailingTextStyleResourceKey = string.IsNullOrWhiteSpace(TrailingTextStyleResourceKey)
                ? "M3ChipLabel"
                : TrailingTextStyleResourceKey;
            string leadingIconFrameStyleResourceKey = string.IsNullOrWhiteSpace(LeadingIconFrameStyleResourceKey)
                ? "M3ActivityThumbnailFrame"
                : LeadingIconFrameStyleResourceKey;
            string actionIconButtonStyleResourceKey = string.IsNullOrWhiteSpace(ActionIconButtonStyleResourceKey)
                ? "M3FileChromeIconButton"
                : ActionIconButtonStyleResourceKey;
            bool isLeadingIconVisible = IsLeadingIconVisible && LeadingIconData is not null;
            bool isTrailingTextVisible = IsTrailingTextVisible && !string.IsNullOrWhiteSpace(trailingText);
            ICommand? actionCommand = Command;
            object? actionCommandParameter = CommandParameter;
            ICommand? rowTapCommand = RowTapCommand ?? actionCommand;
            object? rowTapCommandParameter = RowTapCommand is null
                ? actionCommandParameter
                : RowTapCommandParameter;

            _container.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _leadingIcon.SetDynamicResource(StyleProperty, leadingIconFrameStyleResourceKey);
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _text.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _supportingText.SetDynamicResource(StyleProperty, supportingTextStyleResourceKey);
            _trailingChip.SetDynamicResource(StyleProperty, trailingChipStyleResourceKey);
            _trailingText.SetDynamicResource(StyleProperty, trailingTextStyleResourceKey);
            _actionButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);

            _leadingIcon.IconData = LeadingIconData;
            _leadingIcon.IsVisible = isLeadingIconVisible;
            _text.Text = text;
            _supportingText.Text = supportingText;
            _supportingText.IsVisible = IsSupportingTextVisible && !string.IsNullOrWhiteSpace(supportingText);
            Grid.SetColumn(_textStack, isLeadingIconVisible ? 1 : 0);
            Grid.SetColumnSpan(_textStack, ResolveTextColumnSpan(isLeadingIconVisible, isTrailingTextVisible));

            _trailingText.Text = trailingText;
            _trailingChip.IsVisible = isTrailingTextVisible;

            _actionButton.IconData = ActionIconData;
            _actionButton.Command = actionCommand;
            _actionButton.CommandParameter = actionCommandParameter;
            _actionButton.IsEnabled = IsActionEnabled;
            SemanticProperties.SetDescription(_actionButton, actionSemanticDescription);

            _tapBehavior.TapCommand = IsActionEnabled ? rowTapCommand : null;
            _tapBehavior.TapCommandParameter = rowTapCommandParameter;
            _touchSurface.IsVisible = IsRowTapEnabled && IsActionEnabled && rowTapCommand is not null;
            SemanticProperties.SetDescription(_container, semanticDescription);
        }

        private int ResolveTextColumnSpan(bool isLeadingIconVisible, bool isTrailingTextVisible)
        {
            if (isLeadingIconVisible)
            {
                return isTrailingTextVisible ? 1 : 2;
            }

            return isTrailingTextVisible ? 2 : 3;
        }

        private string CreateSemanticDescription(string text, string supportingText, string trailingText)
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

            if (!string.IsNullOrWhiteSpace(trailingText))
            {
                semanticParts.Add(trailingText);
            }

            return string.Join(". ", semanticParts);
        }
    }
}
