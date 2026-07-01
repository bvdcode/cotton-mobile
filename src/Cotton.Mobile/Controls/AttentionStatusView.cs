// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Behaviors;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class AttentionStatusView : ContentView
    {
        public static readonly BindableProperty MessageProperty = BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(AttentionStatusView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(AttentionStatusView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconDataProperty = BindableProperty.Create(
            nameof(ActionIconData),
            typeof(Geometry),
            typeof(AttentionStatusView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
            nameof(ActionCommand),
            typeof(ICommand),
            typeof(AttentionStatusView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionEnabledProperty = BindableProperty.Create(
            nameof(IsActionEnabled),
            typeof(bool),
            typeof(AttentionStatusView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(AttentionStatusView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsRowTapEnabledProperty = BindableProperty.Create(
            nameof(IsRowTapEnabled),
            typeof(bool),
            typeof(AttentionStatusView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(ActionSemanticDescription),
            typeof(string),
            typeof(AttentionStatusView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PanelStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PanelStyleResourceKey),
            typeof(string),
            typeof(AttentionStatusView),
            "M3AttentionStatusPanel",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(AttentionStatusView),
            "M3StatusGrid",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconStyleResourceKey),
            typeof(string),
            typeof(AttentionStatusView),
            "M3AttentionStatusIcon",
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(AttentionStatusView),
            "M3FileChromeIconButton",
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private readonly Grid _contentGrid;
        private readonly IconView _icon;
        private readonly Label _message;
        private readonly Border _panel;
        private readonly LongPressBehavior _tapBehavior;
        private readonly Grid _touchSurface;

        public AttentionStatusView()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, "M3AttentionStatusIcon");

            _message = new Label();
            _message.SetDynamicResource(StyleProperty, "M3AttentionStatusMessage");

            _tapBehavior = new LongPressBehavior();

            _touchSurface = new Grid();
            _touchSurface.SetDynamicResource(StyleProperty, "M3ListItemTouchSurface");
            _touchSurface.Behaviors.Add(_tapBehavior);
            Grid.SetColumnSpan(_touchSurface, 3);

            _actionButton = new IconButton();
            Grid.SetColumn(_actionButton, 2);

            _contentGrid = new Grid
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
                    _icon,
                    _message,
                    _touchSurface,
                    _actionButton,
                },
            };
            _contentGrid.SetDynamicResource(StyleProperty, "M3StatusGrid");
            Grid.SetColumn(_message, 1);

            _panel = new Border
            {
                Content = _contentGrid,
            };
            _panel.SetDynamicResource(StyleProperty, "M3AttentionStatusPanel");

            Content = _panel;
            UpdateVisualState();
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
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

        public bool IsActionEnabled
        {
            get => (bool)GetValue(IsActionEnabledProperty);
            set => SetValue(IsActionEnabledProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public bool IsRowTapEnabled
        {
            get => (bool)GetValue(IsRowTapEnabledProperty);
            set => SetValue(IsRowTapEnabledProperty, value);
        }

        public string ActionSemanticDescription
        {
            get => (string)GetValue(ActionSemanticDescriptionProperty);
            set => SetValue(ActionSemanticDescriptionProperty, value);
        }

        public string PanelStyleResourceKey
        {
            get => (string)GetValue(PanelStyleResourceKeyProperty);
            set => SetValue(PanelStyleResourceKeyProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string IconStyleResourceKey
        {
            get => (string)GetValue(IconStyleResourceKeyProperty);
            set => SetValue(IconStyleResourceKeyProperty, value);
        }

        public string ActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(ActionIconButtonStyleResourceKeyProperty);
            set => SetValue(ActionIconButtonStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AttentionStatusView attentionStatusView = (AttentionStatusView)bindable;
            attentionStatusView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string message = Message ?? string.Empty;
            string actionSemanticDescription = ActionSemanticDescription ?? string.Empty;
            string panelStyleResourceKey = string.IsNullOrWhiteSpace(PanelStyleResourceKey)
                ? "M3AttentionStatusPanel"
                : PanelStyleResourceKey;
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? "M3StatusGrid"
                : GridStyleResourceKey;
            string iconStyleResourceKey = string.IsNullOrWhiteSpace(IconStyleResourceKey)
                ? "M3AttentionStatusIcon"
                : IconStyleResourceKey;
            string actionIconButtonStyleResourceKey = string.IsNullOrWhiteSpace(ActionIconButtonStyleResourceKey)
                ? "M3FileChromeIconButton"
                : ActionIconButtonStyleResourceKey;
            ICommand? actionCommand = ActionCommand;

            _panel.SetDynamicResource(StyleProperty, panelStyleResourceKey);
            _contentGrid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _icon.SetDynamicResource(StyleProperty, iconStyleResourceKey);
            _actionButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);

            _icon.IconData = IconData;
            _message.Text = message;
            _actionButton.IconData = ActionIconData;
            _actionButton.Command = actionCommand;
            _actionButton.IsEnabled = IsActionEnabled;
            _actionButton.IsVisible = IsActionVisible;
            SemanticProperties.SetDescription(_actionButton, actionSemanticDescription);

            _tapBehavior.TapCommand = actionCommand;
            _touchSurface.IsVisible = IsRowTapEnabled;
            SemanticProperties.SetDescription(_panel, message);
        }
    }
}
