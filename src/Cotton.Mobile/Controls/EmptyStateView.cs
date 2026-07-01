// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Behaviors;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class EmptyStateView : ContentView
    {
        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(EmptyStateView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BodyProperty = BindableProperty.Create(
            nameof(Body),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsBodyVisibleProperty = BindableProperty.Create(
            nameof(IsBodyVisible),
            typeof(bool),
            typeof(EmptyStateView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CardStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CardStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            "M3EmptyStateCard",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconFrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconFrameStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            "M3EmptyStateIconFrame",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
            nameof(ActionText),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconDataProperty = BindableProperty.Create(
            nameof(ActionIconData),
            typeof(Geometry),
            typeof(EmptyStateView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(EmptyStateView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(EmptyStateView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
            typeof(string),
            typeof(EmptyStateView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            "M3EmptyStateActionIconButton",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionRowStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionRowStyleResourceKey),
            typeof(string),
            typeof(EmptyStateView),
            "M3PanelActionListItemGrid",
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private readonly IconButton _actionIconOnlyButton;
        private readonly Label _actionLabel;
        private readonly Grid _actionRow;
        private readonly LongPressBehavior _actionTapBehavior;
        private readonly Grid _actionTouchSurface;
        private readonly Border _card;
        private readonly IconView _icon;
        private readonly Border _iconFrame;
        private readonly Label _title;
        private readonly Label _body;

        public EmptyStateView()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, "M3EmptyStateIcon");

            _iconFrame = new Border
            {
                Content = _icon,
            };

            _title = new Label();
            _title.SetDynamicResource(StyleProperty, "M3EmptyTitle");

            _body = new Label();
            _body.SetDynamicResource(StyleProperty, "M3EmptyBody");

            _actionLabel = new Label();
            _actionLabel.SetDynamicResource(StyleProperty, "M3ActionListItemLabel");

            _actionTapBehavior = new LongPressBehavior();

            _actionTouchSurface = new Grid();
            _actionTouchSurface.SetDynamicResource(StyleProperty, "M3ListItemTouchSurface");
            _actionTouchSurface.Behaviors.Add(_actionTapBehavior);
            Grid.SetColumnSpan(_actionTouchSurface, 2);

            _actionButton = new IconButton();
            Grid.SetColumn(_actionButton, 1);

            _actionRow = new Grid
            {
                ColumnDefinitions =
                {
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
                    _actionLabel,
                    _actionTouchSurface,
                    _actionButton,
                },
            };

            _actionIconOnlyButton = new IconButton();

            VerticalStackLayout stack = new()
            {
                Children =
                {
                    _iconFrame,
                    _title,
                    _body,
                    _actionRow,
                    _actionIconOnlyButton,
                },
            };
            stack.SetDynamicResource(StyleProperty, "M3EmptyStateStack");

            _card = new Border
            {
                Content = stack,
            };
            _card.SetDynamicResource(StyleProperty, "M3EmptyStateCard");

            Content = _card;
            UpdateVisualState();
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Body
        {
            get => (string)GetValue(BodyProperty);
            set => SetValue(BodyProperty, value);
        }

        public bool IsBodyVisible
        {
            get => (bool)GetValue(IsBodyVisibleProperty);
            set => SetValue(IsBodyVisibleProperty, value);
        }

        public string CardStyleResourceKey
        {
            get => (string)GetValue(CardStyleResourceKeyProperty);
            set => SetValue(CardStyleResourceKeyProperty, value);
        }

        public string IconFrameStyleResourceKey
        {
            get => (string)GetValue(IconFrameStyleResourceKeyProperty);
            set => SetValue(IconFrameStyleResourceKeyProperty, value);
        }

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public Geometry? ActionIconData
        {
            get => (Geometry?)GetValue(ActionIconDataProperty);
            set => SetValue(ActionIconDataProperty, value);
        }

        public ICommand? ActionCommand
        {
            get => (ICommand?)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
        }

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        public string ActionRowStyleResourceKey
        {
            get => (string)GetValue(ActionRowStyleResourceKeyProperty);
            set => SetValue(ActionRowStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            EmptyStateView emptyStateView = (EmptyStateView)bindable;
            emptyStateView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string title = Title ?? string.Empty;
            string body = Body ?? string.Empty;
            string actionText = ActionText ?? string.Empty;
            string actionSemanticDescription = ActionSemanticDescription ?? string.Empty;
            string cardStyleResourceKey = string.IsNullOrWhiteSpace(CardStyleResourceKey)
                ? "M3EmptyStateCard"
                : CardStyleResourceKey;
            string iconFrameStyleResourceKey = string.IsNullOrWhiteSpace(IconFrameStyleResourceKey)
                ? "M3EmptyStateIconFrame"
                : IconFrameStyleResourceKey;
            string actionRowStyleResourceKey = string.IsNullOrWhiteSpace(ActionRowStyleResourceKey)
                ? "M3PanelActionListItemGrid"
                : ActionRowStyleResourceKey;
            string actionIconButtonStyleResourceKey = string.IsNullOrWhiteSpace(ActionIconButtonStyleResourceKey)
                ? "M3EmptyStateActionIconButton"
                : ActionIconButtonStyleResourceKey;
            bool isActionTextVisible = IsActionVisible && !string.IsNullOrWhiteSpace(actionText);
            bool isIconOnlyActionVisible = IsActionVisible && string.IsNullOrWhiteSpace(actionText);
            ICommand? actionCommand = ActionCommand;

            _icon.IconData = IconData;
            _title.Text = title;
            _body.Text = body;
            _body.IsVisible = IsBodyVisible && !string.IsNullOrWhiteSpace(body);

            _card.SetDynamicResource(StyleProperty, cardStyleResourceKey);
            _iconFrame.SetDynamicResource(StyleProperty, iconFrameStyleResourceKey);
            _actionRow.SetDynamicResource(StyleProperty, actionRowStyleResourceKey);
            _actionButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);
            _actionIconOnlyButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);

            _actionLabel.Text = actionText;
            _actionButton.IconData = ActionIconData;
            _actionButton.Command = actionCommand;
            SemanticProperties.SetDescription(_actionButton, actionSemanticDescription);
            _actionIconOnlyButton.IconData = ActionIconData;
            _actionIconOnlyButton.Command = actionCommand;
            SemanticProperties.SetDescription(_actionIconOnlyButton, actionSemanticDescription);
            _actionTapBehavior.TapCommand = actionCommand;

            _actionRow.IsVisible = isActionTextVisible;
            _actionIconOnlyButton.IsVisible = isIconOnlyActionVisible;

            string description = !_body.IsVisible
                ? title
                : $"{title}. {body}";
            SemanticProperties.SetDescription(_card, description);
        }
    }
}
