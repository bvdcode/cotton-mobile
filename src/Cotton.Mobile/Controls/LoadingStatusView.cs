// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class LoadingStatusView : ContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(LoadingStatusView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
            nameof(DetailText),
            typeof(string),
            typeof(LoadingStatusView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
            nameof(IsRunning),
            typeof(bool),
            typeof(LoadingStatusView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconDataProperty = BindableProperty.Create(
            nameof(ActionIconData),
            typeof(Geometry),
            typeof(LoadingStatusView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(LoadingStatusView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(LoadingStatusView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(LoadingStatusView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
            typeof(string),
            typeof(LoadingStatusView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ContainerStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ContainerStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            "M3LoadingStatusPanel",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            "M3LoadingStatusGrid",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            "M3CardTextStack",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            "M3LoadingMessage",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailTextStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            "M3CardSupportingBlock",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            "M3FileChromeIconButton",
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private readonly Border _container;
        private readonly Label _detailMessage;
        private readonly Grid _grid;
        private readonly LoadingIndicatorView _loadingIndicator;
        private readonly Label _message;
        private readonly VerticalStackLayout _textStack;

        public LoadingStatusView()
        {
            _loadingIndicator = new LoadingIndicatorView();

            _message = new Label();
            _detailMessage = new Label();

            _textStack = new VerticalStackLayout
            {
                Children =
                {
                    _message,
                    _detailMessage,
                },
            };
            Grid.SetColumn(_textStack, 1);

            _actionButton = new IconButton();
            Grid.SetColumn(_actionButton, 2);

            _grid = new Grid
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
                    _loadingIndicator,
                    _textStack,
                    _actionButton,
                },
            };

            _container = new Border
            {
                Content = _grid,
            };

            Content = _container;
            UpdateVisualState();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string DetailText
        {
            get => (string)GetValue(DetailTextProperty);
            set => SetValue(DetailTextProperty, value);
        }

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            set => SetValue(IsRunningProperty, value);
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

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
        }

        public string ContainerStyleResourceKey
        {
            get => (string)GetValue(ContainerStyleResourceKeyProperty);
            set => SetValue(ContainerStyleResourceKeyProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string TextStackStyleResourceKey
        {
            get => (string)GetValue(TextStackStyleResourceKeyProperty);
            set => SetValue(TextStackStyleResourceKeyProperty, value);
        }

        public string TextStyleResourceKey
        {
            get => (string)GetValue(TextStyleResourceKeyProperty);
            set => SetValue(TextStyleResourceKeyProperty, value);
        }

        public string DetailTextStyleResourceKey
        {
            get => (string)GetValue(DetailTextStyleResourceKeyProperty);
            set => SetValue(DetailTextStyleResourceKeyProperty, value);
        }

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LoadingStatusView view = (LoadingStatusView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            ICommand? actionCommand = ActionCommand;
            bool isActionVisible = IsActionVisible && actionCommand is not null;
            string detailText = DetailText ?? string.Empty;
            string containerStyleResourceKey = string.IsNullOrWhiteSpace(ContainerStyleResourceKey)
                ? "M3LoadingStatusPanel"
                : ContainerStyleResourceKey;
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? "M3LoadingStatusGrid"
                : GridStyleResourceKey;
            string textStackStyleResourceKey = string.IsNullOrWhiteSpace(TextStackStyleResourceKey)
                ? "M3CardTextStack"
                : TextStackStyleResourceKey;
            string textStyleResourceKey = string.IsNullOrWhiteSpace(TextStyleResourceKey)
                ? "M3LoadingMessage"
                : TextStyleResourceKey;
            string detailTextStyleResourceKey = string.IsNullOrWhiteSpace(DetailTextStyleResourceKey)
                ? "M3CardSupportingBlock"
                : DetailTextStyleResourceKey;
            string actionIconButtonStyleResourceKey = string.IsNullOrWhiteSpace(ActionIconButtonStyleResourceKey)
                ? "M3FileChromeIconButton"
                : ActionIconButtonStyleResourceKey;

            _container.SetDynamicResource(StyleProperty, containerStyleResourceKey);
            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _message.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _detailMessage.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _actionButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);

            _loadingIndicator.IsRunning = IsRunning;
            _message.Text = Text ?? string.Empty;
            _detailMessage.Text = detailText;
            _detailMessage.IsVisible = !string.IsNullOrWhiteSpace(detailText);
            _actionButton.IconData = ActionIconData;
            _actionButton.Command = actionCommand;
            _actionButton.IsEnabled = IsActionEnabled;
            _actionButton.IsVisible = isActionVisible;
            SemanticProperties.SetDescription(_actionButton, ActionSemanticDescription ?? string.Empty);
        }
    }
}
