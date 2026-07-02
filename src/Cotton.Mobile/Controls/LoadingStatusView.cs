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

        private readonly IconButton _actionButton;
        private readonly Border _container;
        private readonly Grid _grid;
        private readonly LoadingIndicatorView _loadingIndicator;
        private readonly Label _message;

        public LoadingStatusView()
        {
            _loadingIndicator = new LoadingIndicatorView();

            _message = new Label();
            Grid.SetColumn(_message, 1);

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
                    _message,
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LoadingStatusView view = (LoadingStatusView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            ICommand? actionCommand = ActionCommand;
            bool isActionVisible = IsActionVisible && actionCommand is not null;

            _container.SetDynamicResource(StyleProperty, "M3LoadingStatusPanel");
            _grid.SetDynamicResource(StyleProperty, "M3LoadingStatusGrid");
            _message.SetDynamicResource(StyleProperty, "M3LoadingMessage");
            _actionButton.SetDynamicResource(StyleProperty, "M3FileChromeIconButton");

            _loadingIndicator.IsRunning = IsRunning;
            _message.Text = Text ?? string.Empty;
            _actionButton.IconData = ActionIconData;
            _actionButton.Command = actionCommand;
            _actionButton.IsEnabled = IsActionEnabled;
            _actionButton.IsVisible = isActionVisible;
            SemanticProperties.SetDescription(_actionButton, ActionSemanticDescription ?? string.Empty);
        }
    }
}
