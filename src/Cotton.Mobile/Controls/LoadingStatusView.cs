// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class LoadingStatusView : ContentView
    {
        private const string ActionButtonOpacityAnimationName = "M3LoadingStatusActionButtonOpacity";
        private const string DefaultActionIconButtonStyleResourceKey = "M3FileChromeIconButton";
        private const string DefaultContainerStyleResourceKey = "M3LoadingStatusPanel";
        private const string DefaultDetailTextStyleResourceKey = "M3CardSupportingBlock";
        private const string DefaultGridStyleResourceKey = "M3LoadingStatusGrid";
        private const string DefaultTextStackStyleResourceKey = "M3CardTextStack";
        private const string DefaultTextStyleResourceKey = "M3LoadingMessage";
        private const string DetailMessageOpacityAnimationName = "M3LoadingStatusDetailMessageOpacity";
        private const string StatusOpacityAnimationName = "M3LoadingStatusOpacity";

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
            propertyChanged: OnDetailMessageVisibilityPropertyChanged);

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
            propertyChanged: OnStatusVisiblePropertyChanged);

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
            propertyChanged: OnActionButtonVisibilityPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(LoadingStatusView),
            false,
            propertyChanged: OnActionButtonVisibilityPropertyChanged);

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
            DefaultContainerStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStackStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            DefaultTextStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            DefaultTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty DetailTextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(DetailTextStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            DefaultDetailTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(LoadingStatusView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _actionButton;
        private readonly Border _container;
        private readonly Label _detailMessage;
        private readonly Grid _grid;
        private readonly LoadingIndicatorView _loadingIndicator;
        private readonly Label _message;
        private readonly VerticalStackLayout _textStack;
        private bool _hasAppliedActionButtonVisibility;
        private bool _hasAppliedDetailMessageVisibility;
        private bool _hasAppliedStatusVisibility;

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
            UpdateVisualState(animateDetailMessageVisibility: false, animateActionButtonVisibility: false);
            UpdateStatusVisibility(animateStatusVisibility: false);
            UpdateInputTransparency();
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
            view.UpdateVisualState(animateDetailMessageVisibility: false, animateActionButtonVisibility: false);
        }

        private static void OnStatusVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LoadingStatusView view = (LoadingStatusView)bindable;
            view.UpdateStatusVisibility(animateStatusVisibility: true);
        }

        private static void OnDetailMessageVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            LoadingStatusView view = (LoadingStatusView)bindable;
            view.UpdateVisualState(animateDetailMessageVisibility: true, animateActionButtonVisibility: false);
        }

        private static void OnActionButtonVisibilityPropertyChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            LoadingStatusView view = (LoadingStatusView)bindable;
            view.UpdateVisualState(animateDetailMessageVisibility: false, animateActionButtonVisibility: true);
        }

        private void UpdateVisualState(bool animateDetailMessageVisibility, bool animateActionButtonVisibility)
        {
            ICommand? actionCommand = ActionCommand;
            bool isActionVisible = IsActionVisible && actionCommand is not null;
            string detailText = DetailText ?? string.Empty;
            string containerStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ContainerStyleResourceKey,
                DefaultContainerStyleResourceKey);
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                GridStyleResourceKey,
                DefaultGridStyleResourceKey);
            string textStackStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                TextStackStyleResourceKey,
                DefaultTextStackStyleResourceKey);
            string textStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                TextStyleResourceKey,
                DefaultTextStyleResourceKey);
            string detailTextStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                DetailTextStyleResourceKey,
                DefaultDetailTextStyleResourceKey);
            string actionIconButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ActionIconButtonStyleResourceKey,
                DefaultActionIconButtonStyleResourceKey);

            _container.SetDynamicResource(StyleProperty, containerStyleResourceKey);
            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _textStack.SetDynamicResource(StyleProperty, textStackStyleResourceKey);
            _message.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _detailMessage.SetDynamicResource(StyleProperty, detailTextStyleResourceKey);
            _actionButton.SetDynamicResource(StyleProperty, actionIconButtonStyleResourceKey);

            _loadingIndicator.IsRunning = IsRunning;
            _message.Text = Text ?? string.Empty;
            _detailMessage.Text = detailText;
            UpdateDetailMessageVisibility(detailText, animateDetailMessageVisibility);
            _actionButton.IconData = ActionIconData;
            _actionButton.Command = actionCommand;
            _actionButton.IsEnabled = IsActionEnabled;
            UpdateActionButtonVisibility(isActionVisible, animateActionButtonVisibility);
            SemanticProperties.SetDescription(_actionButton, ActionSemanticDescription ?? string.Empty);
            UpdateInputTransparency();
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
            else
            {
                UpdateInputTransparency();
            }

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

        private void UpdateDetailMessageVisibility(string detailText, bool animateDetailMessageVisibility)
        {
            bool isDetailMessageVisible = IsDetailMessageActuallyVisible(detailText);
            bool shouldAnimate = animateDetailMessageVisibility && _hasAppliedDetailMessageVisibility;
            double targetOpacity = isDetailMessageVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isDetailMessageVisible)
            {
                _detailMessage.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                _detailMessage,
                _detailMessage.Opacity,
                targetOpacity,
                duration,
                DetailMessageOpacityAnimationName,
                shouldAnimate,
                opacity => _detailMessage.Opacity = opacity,
                CompleteDetailMessageVisibility);
            _hasAppliedDetailMessageVisibility = true;
        }

        private void UpdateActionButtonVisibility(bool isActionVisible, bool animateActionButtonVisibility)
        {
            bool shouldAnimate = animateActionButtonVisibility && _hasAppliedActionButtonVisibility;
            double targetOpacity = isActionVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isActionVisible)
            {
                _actionButton.IsVisible = true;
            }
            else
            {
                _actionButton.InputTransparent = true;
            }

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
            UpdateInputTransparency();
        }

        private void CompleteDetailMessageVisibility()
        {
            if (IsDetailMessageActuallyVisible(DetailText ?? string.Empty))
            {
                _detailMessage.IsVisible = true;
                return;
            }

            _detailMessage.IsVisible = false;
        }

        private void CompleteActionButtonVisibility()
        {
            if (IsActionButtonActuallyVisible(ActionCommand))
            {
                _actionButton.IsVisible = true;
                _actionButton.InputTransparent = false;
                UpdateInputTransparency();
                return;
            }

            _actionButton.IsVisible = false;
            _actionButton.InputTransparent = true;
            UpdateInputTransparency();
        }

        private static bool IsDetailMessageActuallyVisible(string detailText)
        {
            return !string.IsNullOrWhiteSpace(detailText);
        }

        private bool IsActionButtonActuallyVisible(ICommand? actionCommand)
        {
            return IsActionVisible && actionCommand is not null;
        }

        private void UpdateInputTransparency()
        {
            InputTransparent = !IsVisible || !IsStatusVisible || !IsActionButtonActuallyVisible(ActionCommand);
        }
    }
}
