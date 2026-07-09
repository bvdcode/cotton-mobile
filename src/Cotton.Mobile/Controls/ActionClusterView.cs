// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class ActionClusterView : HorizontalStackLayout
    {
        private const string DefaultActionIconButtonStyleResourceKey = "M3DefaultIconButton";
        private const string DefaultClusterStyleResourceKey = "M3RowActionCluster";
        private const string ClusterOpacityAnimationName = "M3ActionClusterOpacity";
        private const string PrimaryActionButtonOpacityAnimationName = "M3ActionClusterPrimaryButtonOpacity";
        private const string SecondaryActionButtonOpacityAnimationName = "M3ActionClusterSecondaryButtonOpacity";
        private const string TertiaryActionButtonOpacityAnimationName = "M3ActionClusterTertiaryButtonOpacity";
        private const string QuaternaryActionButtonOpacityAnimationName = "M3ActionClusterQuaternaryButtonOpacity";

        public static readonly BindableProperty ClusterStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ClusterStyleResourceKey),
            typeof(string),
            typeof(ActionClusterView),
            DefaultClusterStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsClusterVisibleProperty = BindableProperty.Create(
            nameof(IsClusterVisible),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnClusterVisiblePropertyChanged);

        public static readonly BindableProperty PrimaryActionIconDataProperty = BindableProperty.Create(
            nameof(PrimaryActionIconData),
            typeof(Geometry),
            typeof(ActionClusterView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionCommandProperty = BindableProperty.Create(
            nameof(PrimaryActionCommand),
            typeof(ICommand),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionCommandParameterProperty = BindableProperty.Create(
            nameof(PrimaryActionCommandParameter),
            typeof(object),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(PrimaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(ActionClusterView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PrimaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(PrimaryActionSemanticDescription),
            typeof(string),
            typeof(ActionClusterView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPrimaryActionEnabledProperty = BindableProperty.Create(
            nameof(IsPrimaryActionEnabled),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsPrimaryActionVisibleProperty = BindableProperty.Create(
            nameof(IsPrimaryActionVisible),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnActionVisibilityPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconDataProperty = BindableProperty.Create(
            nameof(SecondaryActionIconData),
            typeof(Geometry),
            typeof(ActionClusterView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionCommandProperty = BindableProperty.Create(
            nameof(SecondaryActionCommand),
            typeof(ICommand),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionCommandParameterProperty = BindableProperty.Create(
            nameof(SecondaryActionCommandParameter),
            typeof(object),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SecondaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(ActionClusterView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SecondaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(SecondaryActionSemanticDescription),
            typeof(string),
            typeof(ActionClusterView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSecondaryActionEnabledProperty = BindableProperty.Create(
            nameof(IsSecondaryActionEnabled),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsSecondaryActionVisibleProperty = BindableProperty.Create(
            nameof(IsSecondaryActionVisible),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnActionVisibilityPropertyChanged);

        public static readonly BindableProperty TertiaryActionIconDataProperty = BindableProperty.Create(
            nameof(TertiaryActionIconData),
            typeof(Geometry),
            typeof(ActionClusterView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionCommandProperty = BindableProperty.Create(
            nameof(TertiaryActionCommand),
            typeof(ICommand),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionCommandParameterProperty = BindableProperty.Create(
            nameof(TertiaryActionCommandParameter),
            typeof(object),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TertiaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(ActionClusterView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TertiaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(TertiaryActionSemanticDescription),
            typeof(string),
            typeof(ActionClusterView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsTertiaryActionEnabledProperty = BindableProperty.Create(
            nameof(IsTertiaryActionEnabled),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsTertiaryActionVisibleProperty = BindableProperty.Create(
            nameof(IsTertiaryActionVisible),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnActionVisibilityPropertyChanged);

        public static readonly BindableProperty QuaternaryActionIconDataProperty = BindableProperty.Create(
            nameof(QuaternaryActionIconData),
            typeof(Geometry),
            typeof(ActionClusterView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty QuaternaryActionCommandProperty = BindableProperty.Create(
            nameof(QuaternaryActionCommand),
            typeof(ICommand),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty QuaternaryActionCommandParameterProperty = BindableProperty.Create(
            nameof(QuaternaryActionCommandParameter),
            typeof(object),
            typeof(ActionClusterView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty QuaternaryActionIconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(QuaternaryActionIconButtonStyleResourceKey),
            typeof(string),
            typeof(ActionClusterView),
            DefaultActionIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty QuaternaryActionSemanticDescriptionProperty = BindableProperty.Create(
            nameof(QuaternaryActionSemanticDescription),
            typeof(string),
            typeof(ActionClusterView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsQuaternaryActionEnabledProperty = BindableProperty.Create(
            nameof(IsQuaternaryActionEnabled),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsQuaternaryActionVisibleProperty = BindableProperty.Create(
            nameof(IsQuaternaryActionVisible),
            typeof(bool),
            typeof(ActionClusterView),
            true,
            propertyChanged: OnActionVisibilityPropertyChanged);

        private readonly IconButton _primaryActionButton;
        private readonly IconButton _quaternaryActionButton;
        private readonly IconButton _secondaryActionButton;
        private readonly IconButton _tertiaryActionButton;
        private bool _hasAppliedActionVisibilityState;
        private bool _hasAppliedClusterVisibility;

        public ActionClusterView()
        {
            _primaryActionButton = new IconButton();
            _secondaryActionButton = new IconButton();
            _tertiaryActionButton = new IconButton();
            _quaternaryActionButton = new IconButton();

            Children.Add(_primaryActionButton);
            Children.Add(_secondaryActionButton);
            Children.Add(_tertiaryActionButton);
            Children.Add(_quaternaryActionButton);
            UpdateVisualState(animateActionVisibility: false);
            UpdateClusterVisibility(animateClusterVisibility: false);
        }

        public string ClusterStyleResourceKey
        {
            get => (string)GetValue(ClusterStyleResourceKeyProperty);
            set => SetValue(ClusterStyleResourceKeyProperty, value);
        }

        public bool IsClusterVisible
        {
            get => (bool)GetValue(IsClusterVisibleProperty);
            set => SetValue(IsClusterVisibleProperty, value);
        }

        public Geometry? PrimaryActionIconData
        {
            get => (Geometry?)GetValue(PrimaryActionIconDataProperty);
            set => SetValue(PrimaryActionIconDataProperty, value);
        }

        public ICommand? PrimaryActionCommand
        {
            get => (ICommand?)GetValue(PrimaryActionCommandProperty);
            set => SetValue(PrimaryActionCommandProperty, value);
        }

        public object? PrimaryActionCommandParameter
        {
            get => GetValue(PrimaryActionCommandParameterProperty);
            set => SetValue(PrimaryActionCommandParameterProperty, value);
        }

        public string PrimaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(PrimaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(PrimaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public string PrimaryActionSemanticDescription
        {
            get => (string)GetValue(PrimaryActionSemanticDescriptionProperty);
            set => SetValue(PrimaryActionSemanticDescriptionProperty, value);
        }

        public bool IsPrimaryActionEnabled
        {
            get => (bool)GetValue(IsPrimaryActionEnabledProperty);
            set => SetValue(IsPrimaryActionEnabledProperty, value);
        }

        public bool IsPrimaryActionVisible
        {
            get => (bool)GetValue(IsPrimaryActionVisibleProperty);
            set => SetValue(IsPrimaryActionVisibleProperty, value);
        }

        public Geometry? SecondaryActionIconData
        {
            get => (Geometry?)GetValue(SecondaryActionIconDataProperty);
            set => SetValue(SecondaryActionIconDataProperty, value);
        }

        public ICommand? SecondaryActionCommand
        {
            get => (ICommand?)GetValue(SecondaryActionCommandProperty);
            set => SetValue(SecondaryActionCommandProperty, value);
        }

        public object? SecondaryActionCommandParameter
        {
            get => GetValue(SecondaryActionCommandParameterProperty);
            set => SetValue(SecondaryActionCommandParameterProperty, value);
        }

        public string SecondaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(SecondaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(SecondaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public string SecondaryActionSemanticDescription
        {
            get => (string)GetValue(SecondaryActionSemanticDescriptionProperty);
            set => SetValue(SecondaryActionSemanticDescriptionProperty, value);
        }

        public bool IsSecondaryActionEnabled
        {
            get => (bool)GetValue(IsSecondaryActionEnabledProperty);
            set => SetValue(IsSecondaryActionEnabledProperty, value);
        }

        public bool IsSecondaryActionVisible
        {
            get => (bool)GetValue(IsSecondaryActionVisibleProperty);
            set => SetValue(IsSecondaryActionVisibleProperty, value);
        }

        public Geometry? TertiaryActionIconData
        {
            get => (Geometry?)GetValue(TertiaryActionIconDataProperty);
            set => SetValue(TertiaryActionIconDataProperty, value);
        }

        public ICommand? TertiaryActionCommand
        {
            get => (ICommand?)GetValue(TertiaryActionCommandProperty);
            set => SetValue(TertiaryActionCommandProperty, value);
        }

        public object? TertiaryActionCommandParameter
        {
            get => GetValue(TertiaryActionCommandParameterProperty);
            set => SetValue(TertiaryActionCommandParameterProperty, value);
        }

        public string TertiaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(TertiaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(TertiaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public string TertiaryActionSemanticDescription
        {
            get => (string)GetValue(TertiaryActionSemanticDescriptionProperty);
            set => SetValue(TertiaryActionSemanticDescriptionProperty, value);
        }

        public bool IsTertiaryActionEnabled
        {
            get => (bool)GetValue(IsTertiaryActionEnabledProperty);
            set => SetValue(IsTertiaryActionEnabledProperty, value);
        }

        public bool IsTertiaryActionVisible
        {
            get => (bool)GetValue(IsTertiaryActionVisibleProperty);
            set => SetValue(IsTertiaryActionVisibleProperty, value);
        }

        public Geometry? QuaternaryActionIconData
        {
            get => (Geometry?)GetValue(QuaternaryActionIconDataProperty);
            set => SetValue(QuaternaryActionIconDataProperty, value);
        }

        public ICommand? QuaternaryActionCommand
        {
            get => (ICommand?)GetValue(QuaternaryActionCommandProperty);
            set => SetValue(QuaternaryActionCommandProperty, value);
        }

        public object? QuaternaryActionCommandParameter
        {
            get => GetValue(QuaternaryActionCommandParameterProperty);
            set => SetValue(QuaternaryActionCommandParameterProperty, value);
        }

        public string QuaternaryActionIconButtonStyleResourceKey
        {
            get => (string)GetValue(QuaternaryActionIconButtonStyleResourceKeyProperty);
            set => SetValue(QuaternaryActionIconButtonStyleResourceKeyProperty, value);
        }

        public string QuaternaryActionSemanticDescription
        {
            get => (string)GetValue(QuaternaryActionSemanticDescriptionProperty);
            set => SetValue(QuaternaryActionSemanticDescriptionProperty, value);
        }

        public bool IsQuaternaryActionEnabled
        {
            get => (bool)GetValue(IsQuaternaryActionEnabledProperty);
            set => SetValue(IsQuaternaryActionEnabledProperty, value);
        }

        public bool IsQuaternaryActionVisible
        {
            get => (bool)GetValue(IsQuaternaryActionVisibleProperty);
            set => SetValue(IsQuaternaryActionVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ActionClusterView view = (ActionClusterView)bindable;
            view.UpdateVisualState(animateActionVisibility: false);
        }

        private static void OnActionVisibilityPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ActionClusterView view = (ActionClusterView)bindable;
            view.UpdateVisualState(animateActionVisibility: true);
        }

        private static void OnClusterVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ActionClusterView view = (ActionClusterView)bindable;
            view.UpdateClusterVisibility(animateClusterVisibility: true);
        }

        private void UpdateVisualState(bool animateActionVisibility)
        {
            bool shouldAnimateVisibility = animateActionVisibility && _hasAppliedActionVisibilityState;
            string clusterStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ClusterStyleResourceKey,
                DefaultClusterStyleResourceKey);

            SetDynamicResource(StyleProperty, clusterStyleResourceKey);

            UpdateActionButton(
                _primaryActionButton,
                PrimaryActionIconData,
                PrimaryActionCommand,
                PrimaryActionCommandParameter,
                PrimaryActionIconButtonStyleResourceKey,
                PrimaryActionSemanticDescription ?? string.Empty,
                IsPrimaryActionEnabled,
                IsPrimaryActionVisible,
                PrimaryActionButtonOpacityAnimationName,
                shouldAnimateVisibility);
            UpdateActionButton(
                _secondaryActionButton,
                SecondaryActionIconData,
                SecondaryActionCommand,
                SecondaryActionCommandParameter,
                SecondaryActionIconButtonStyleResourceKey,
                SecondaryActionSemanticDescription ?? string.Empty,
                IsSecondaryActionEnabled,
                IsSecondaryActionVisible,
                SecondaryActionButtonOpacityAnimationName,
                shouldAnimateVisibility);
            UpdateActionButton(
                _tertiaryActionButton,
                TertiaryActionIconData,
                TertiaryActionCommand,
                TertiaryActionCommandParameter,
                TertiaryActionIconButtonStyleResourceKey,
                TertiaryActionSemanticDescription ?? string.Empty,
                IsTertiaryActionEnabled,
                IsTertiaryActionVisible,
                TertiaryActionButtonOpacityAnimationName,
                shouldAnimateVisibility);
            UpdateActionButton(
                _quaternaryActionButton,
                QuaternaryActionIconData,
                QuaternaryActionCommand,
                QuaternaryActionCommandParameter,
                QuaternaryActionIconButtonStyleResourceKey,
                QuaternaryActionSemanticDescription ?? string.Empty,
                IsQuaternaryActionEnabled,
                IsQuaternaryActionVisible,
                QuaternaryActionButtonOpacityAnimationName,
                shouldAnimateVisibility);
            _hasAppliedActionVisibilityState = true;
        }

        private void UpdateClusterVisibility(bool animateClusterVisibility)
        {
            bool isClusterVisible = IsClusterVisible;
            bool shouldAnimate = animateClusterVisibility && _hasAppliedClusterVisibility;
            double targetOpacity = isClusterVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            InputTransparent = !isClusterVisible;
            if (isClusterVisible)
            {
                IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                ClusterOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteClusterVisibility);
            _hasAppliedClusterVisibility = true;
        }

        private static void UpdateActionButton(
            IconButton actionButton,
            Geometry? iconData,
            ICommand? command,
            object? commandParameter,
            string iconButtonStyleResourceKey,
            string semanticDescription,
            bool isEnabled,
            bool isVisible,
            string opacityAnimationName,
            bool animateVisibility)
        {
            string styleResourceKey = MaterialResources.ResolveStyleResourceKey(
                iconButtonStyleResourceKey,
                DefaultActionIconButtonStyleResourceKey);
            bool isActionVisible = isVisible && iconData is not null && command is not null;
            double targetOpacity = isActionVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            actionButton.SetDynamicResource(StyleProperty, styleResourceKey);
            actionButton.IconData = iconData;
            actionButton.Command = command;
            actionButton.CommandParameter = commandParameter;
            actionButton.IsEnabled = isEnabled;
            if (isActionVisible)
            {
                actionButton.IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                actionButton,
                actionButton.Opacity,
                targetOpacity,
                duration,
                opacityAnimationName,
                animateVisibility,
                opacity => actionButton.Opacity = opacity,
                () => CompleteActionButtonVisibility(actionButton, isActionVisible));
            SemanticProperties.SetDescription(actionButton, semanticDescription);
        }

        private static void CompleteActionButtonVisibility(IconButton actionButton, bool isActionVisible)
        {
            if (isActionVisible)
            {
                actionButton.IsVisible = true;
                return;
            }

            actionButton.IsVisible = false;
        }

        private void CompleteClusterVisibility()
        {
            IsVisible = IsClusterVisible;
        }
    }
}
