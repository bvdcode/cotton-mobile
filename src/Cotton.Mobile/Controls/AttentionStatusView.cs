// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class AttentionStatusView : ContentView
    {
        private const string ActionButtonOpacityAnimationName = "M3AttentionStatusActionButtonOpacity";
        private const string DefaultActionIconButtonStyleResourceKey = "M3DefaultIconButton";
        private const string DefaultGridStyleResourceKey = "M3StatusGrid";
        private const string DefaultIconStyleResourceKey = "M3AttentionStatusIcon";
        private const string DefaultPanelStyleResourceKey = "M3AttentionStatusPanel";
        private const string StatusOpacityAnimationName = "M3AttentionStatusOpacity";

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
            propertyChanged: OnActionButtonVisibilityPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(AttentionStatusView),
            true,
            propertyChanged: OnStatusVisiblePropertyChanged);

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
            DefaultPanelStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(AttentionStatusView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconStyleResourceKey),
            typeof(string),
            typeof(AttentionStatusView),
            DefaultIconStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(AttentionStatusView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private readonly Grid _contentGrid;
        private readonly IconView _icon;
        private readonly Label _message;
        private readonly Border _panel;
        private readonly TouchSurfaceView _touchSurface;
        private bool _hasAppliedActionButtonVisibility;
        private bool _hasAppliedStatusVisibility;

        public AttentionStatusView()
        {
            _icon = new IconView();
            _icon.SetDynamicResource(StyleProperty, DefaultIconStyleResourceKey);

            _message = new Label();
            _message.SetDynamicResource(StyleProperty, "M3AttentionStatusMessage");

            _touchSurface = new TouchSurfaceView();
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
            UpdateVisualState(animateActionButtonVisibility: false);
            UpdateStatusVisibility(animateStatusVisibility: false);
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

        public bool IsStatusVisible
        {
            get => (bool)GetValue(IsStatusVisibleProperty);
            set => SetValue(IsStatusVisibleProperty, value);
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
            attentionStatusView.UpdateVisualState(animateActionButtonVisibility: false);
        }

        private static void OnStatusVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            AttentionStatusView attentionStatusView = (AttentionStatusView)bindable;
            attentionStatusView.UpdateStatusVisibility(animateStatusVisibility: true);
        }

        private static void OnActionButtonVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            AttentionStatusView attentionStatusView = (AttentionStatusView)bindable;
            attentionStatusView.UpdateVisualState(animateActionButtonVisibility: true);
        }

        private void UpdateVisualState(bool animateActionButtonVisibility)
        {
            string message = Message ?? string.Empty;
            string actionSemanticDescription = ActionSemanticDescription ?? string.Empty;
            string panelStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                PanelStyleResourceKey,
                DefaultPanelStyleResourceKey);
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                GridStyleResourceKey,
                DefaultGridStyleResourceKey);
            string iconStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                IconStyleResourceKey,
                DefaultIconStyleResourceKey);
            string actionIconButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ActionIconButtonStyleResourceKey,
                DefaultActionIconButtonStyleResourceKey);
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
            UpdateActionButtonVisibility(animateActionButtonVisibility);
            SemanticProperties.SetDescription(_actionButton, actionSemanticDescription);

            _touchSurface.TapCommand = IsRowTapEnabled && IsActionEnabled ? actionCommand : null;
            _touchSurface.IsVisible = IsRowTapEnabled && IsActionEnabled && actionCommand is not null;
            SemanticProperties.SetDescription(_panel, message);
        }

        private void UpdateStatusVisibility(bool animateStatusVisibility)
        {
            bool isStatusVisible = IsStatusVisible;
            bool shouldAnimate = animateStatusVisibility && _hasAppliedStatusVisibility;
            double targetOpacity = isStatusVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isStatusVisible)
            {
                IsVisible = true;
            }

            InputTransparent = !isStatusVisible;
            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                StatusOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteStatusVisibility);
            _hasAppliedStatusVisibility = true;
        }

        private void UpdateActionButtonVisibility(bool animateActionButtonVisibility)
        {
            bool shouldAnimate = animateActionButtonVisibility && _hasAppliedActionButtonVisibility;
            double targetOpacity = IsActionVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (IsActionVisible)
            {
                _actionButton.IsVisible = true;
            }

            _actionButton.InputTransparent = !IsActionVisible;
            MaterialMotion.UpdateDouble(
                _actionButton,
                _actionButton.Opacity,
                targetOpacity,
                duration,
                ActionButtonOpacityAnimationName,
                shouldAnimate,
                opacity => _actionButton.Opacity = opacity,
                CompleteActionButtonVisibility);
            _hasAppliedActionButtonVisibility = true;
        }

        private void CompleteStatusVisibility()
        {
            IsVisible = IsStatusVisible;
            InputTransparent = !IsVisible || !IsStatusVisible;
        }

        private void CompleteActionButtonVisibility()
        {
            if (IsActionVisible)
            {
                _actionButton.IsVisible = true;
                _actionButton.InputTransparent = false;
                return;
            }

            _actionButton.IsVisible = false;
            _actionButton.InputTransparent = true;
        }
    }
}
