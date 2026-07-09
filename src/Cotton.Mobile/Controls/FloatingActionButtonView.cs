// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class FloatingActionButtonView : ContentView
    {
        private const string ActionOpacityAnimationName = "M3FloatingActionOpacity";
        private const string DefaultIconButtonStyleResourceKey = "M3FloatingActionIconButton";

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(FloatingActionButtonView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(FloatingActionButtonView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(FloatingActionButtonView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SemanticDescriptionProperty = BindableProperty.Create(
            nameof(SemanticDescription),
            typeof(string),
            typeof(FloatingActionButtonView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsActionVisibleProperty = BindableProperty.Create(
            nameof(IsActionVisible),
            typeof(bool),
            typeof(FloatingActionButtonView),
            true,
            propertyChanged: OnActionVisiblePropertyChanged);

        public static readonly BindableProperty IconButtonStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IconButtonStyleResourceKey),
            typeof(string),
            typeof(FloatingActionButtonView),
            DefaultIconButtonStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly IconButton _button;
        private bool _hasAppliedActionVisibility;

        public FloatingActionButtonView()
        {
            _button = new IconButton();

            Content = _button;
            UpdateVisualState();
            UpdateActionVisibility(animateActionVisibility: false);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
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

        public string SemanticDescription
        {
            get => (string)GetValue(SemanticDescriptionProperty);
            set => SetValue(SemanticDescriptionProperty, value);
        }

        public bool IsActionVisible
        {
            get => (bool)GetValue(IsActionVisibleProperty);
            set => SetValue(IsActionVisibleProperty, value);
        }

        public string IconButtonStyleResourceKey
        {
            get => (string)GetValue(IconButtonStyleResourceKeyProperty);
            set => SetValue(IconButtonStyleResourceKeyProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(IsVisible), StringComparison.Ordinal))
            {
                UpdateVisualState();
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FloatingActionButtonView view = (FloatingActionButtonView)bindable;
            view.UpdateVisualState();
        }

        private static void OnActionVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FloatingActionButtonView view = (FloatingActionButtonView)bindable;
            view.UpdateActionVisibility(animateActionVisibility: true);
        }

        private void UpdateVisualState()
        {
            string iconButtonStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                IconButtonStyleResourceKey,
                DefaultIconButtonStyleResourceKey);

            _button.SetDynamicResource(StyleProperty, iconButtonStyleResourceKey);
            _button.IconData = IconData ?? IconPathData.Plus;
            _button.Command = Command;
            _button.CommandParameter = CommandParameter;
            _button.IsEnabled = IsEnabled;
            SemanticProperties.SetDescription(_button, SemanticDescription ?? string.Empty);
            UpdateInputTransparency();
        }

        private void UpdateActionVisibility(bool animateActionVisibility)
        {
            bool isActionVisible = IsActionVisible;
            bool shouldAnimate = animateActionVisibility && _hasAppliedActionVisibility;
            double targetOpacity = isActionVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isActionVisible)
            {
                IsVisible = true;
            }

            UpdateInputTransparency();
            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                ActionOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteActionVisibility);
            _hasAppliedActionVisibility = true;
        }

        private void CompleteActionVisibility()
        {
            IsVisible = IsActionVisible;
            UpdateInputTransparency();
        }

        private void UpdateInputTransparency()
        {
            InputTransparent = !IsVisible || !IsActionVisible || !IsEnabled || Command is null;
        }
    }
}
