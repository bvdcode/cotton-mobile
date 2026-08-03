// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class LoadingStatusView : ContentView
    {
        private const string DefaultContainerStyle = "M3LoadingStatusPanel";
        private const string DefaultTextStyle = "M3LoadingMessage";

        public static readonly BindableProperty TextProperty = CreateTextProperty(nameof(Text));
        public static readonly BindableProperty DetailTextProperty = CreateTextProperty(nameof(DetailText));
        public static readonly BindableProperty ActionSemanticDescriptionProperty =
            CreateTextProperty(nameof(ActionSemanticDescription));

        public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
            nameof(IsRunning),
            typeof(bool),
            typeof(LoadingStatusView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(LoadingStatusView),
            true,
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

        public static readonly BindableProperty ContainerStyleResourceKeyProperty = CreateStyleProperty(
            nameof(ContainerStyleResourceKey),
            DefaultContainerStyle);

        public static readonly BindableProperty TextStyleResourceKeyProperty = CreateStyleProperty(
            nameof(TextStyleResourceKey),
            DefaultTextStyle);

        private readonly IconButton _actionButton;
        private readonly Border _container;
        private readonly Label _detailMessage;
        private readonly LoadingIndicatorView _loadingIndicator;
        private readonly Label _message;

        public LoadingStatusView()
        {
            _loadingIndicator = new LoadingIndicatorView();
            _message = new Label();
            _detailMessage = new Label();
            _detailMessage.SetDynamicResource(StyleProperty, "M3CardSupportingBlock");

            VerticalStackLayout textStack = new()
            {
                Children =
                {
                    _message,
                    _detailMessage,
                },
            };
            textStack.SetDynamicResource(StyleProperty, "M3CardTextStack");

            _actionButton = new IconButton();
            _actionButton.SetDynamicResource(StyleProperty, "M3DefaultIconButton");

            Grid grid = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                Children =
                {
                    _loadingIndicator,
                    textStack,
                    _actionButton,
                },
            };
            grid.SetDynamicResource(StyleProperty, "M3LoadingStatusGrid");
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(_actionButton, 2);

            _container = new Border { Content = grid };
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

        public bool IsStatusVisible
        {
            get => (bool)GetValue(IsStatusVisibleProperty);
            set => SetValue(IsStatusVisibleProperty, value);
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

        public string TextStyleResourceKey
        {
            get => (string)GetValue(TextStyleResourceKeyProperty);
            set => SetValue(TextStyleResourceKeyProperty, value);
        }

        private static BindableProperty CreateTextProperty(string name)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(LoadingStatusView),
                string.Empty,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static BindableProperty CreateStyleProperty(string name, string defaultValue)
        {
            return BindableProperty.Create(
                name,
                typeof(string),
                typeof(LoadingStatusView),
                defaultValue,
                propertyChanged: OnVisualPropertyChanged);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((LoadingStatusView)bindable).UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_container is null)
            {
                return;
            }

            string text = Text ?? string.Empty;
            string detailText = DetailText ?? string.Empty;
            bool hasAction = IsActionVisible && ActionCommand is not null && ActionIconData is not null;

            IsVisible = IsStatusVisible;
            _container.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(
                    ContainerStyleResourceKey,
                    DefaultContainerStyle));
            _message.SetDynamicResource(
                StyleProperty,
                MaterialResources.ResolveStyleResourceKey(TextStyleResourceKey, DefaultTextStyle));

            _loadingIndicator.IsRunning = IsRunning;
            _message.Text = text;
            _detailMessage.Text = detailText;
            _detailMessage.IsVisible = !string.IsNullOrWhiteSpace(detailText);

            _actionButton.IconData = ActionIconData;
            _actionButton.Command = ActionCommand;
            _actionButton.IsEnabled = IsActionEnabled;
            _actionButton.IsVisible = hasAction;
            SemanticProperties.SetDescription(
                _actionButton,
                ActionSemanticDescription ?? string.Empty);

            InputTransparent = !hasAction;
            SemanticProperties.SetDescription(
                this,
                string.Join(", ", new[] { text, detailText }
                    .Where(value => !string.IsNullOrWhiteSpace(value))));
        }
    }
}
