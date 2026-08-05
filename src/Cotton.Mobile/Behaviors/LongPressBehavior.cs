// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Controls;

namespace Cotton.Mobile.Behaviors
{
    public class LongPressBehavior : Behavior<VisualElement>
    {
        private const string StateLayerAnimationName = "M3ListItemStateLayer";

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(LongPressBehavior));

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(LongPressBehavior));

        public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(LongPressBehavior));

        public static readonly BindableProperty TapCommandParameterProperty = BindableProperty.Create(
            nameof(TapCommandParameter),
            typeof(object),
            typeof(LongPressBehavior));

        public static readonly BindableProperty RestingBackgroundColorProperty = BindableProperty.Create(
            nameof(RestingBackgroundColor),
            typeof(Color),
            typeof(LongPressBehavior),
            default(Color),
            propertyChanged: OnBackgroundColorChanged);

        public static readonly BindableProperty PressedBackgroundColorProperty = BindableProperty.Create(
            nameof(PressedBackgroundColor),
            typeof(Color),
            typeof(LongPressBehavior),
            default(Color),
            propertyChanged: OnBackgroundColorChanged);

        private VisualElement? _visualElement;
        private Application? _application;
        private bool _isPressed;

#if ANDROID
        private AndroidLongPressGesture? _platformGesture;
#endif

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

        public ICommand? TapCommand
        {
            get => (ICommand?)GetValue(TapCommandProperty);
            set => SetValue(TapCommandProperty, value);
        }

        public object? TapCommandParameter
        {
            get => GetValue(TapCommandParameterProperty);
            set => SetValue(TapCommandParameterProperty, value);
        }

        public Color? RestingBackgroundColor
        {
            get => (Color?)GetValue(RestingBackgroundColorProperty);
            set => SetValue(RestingBackgroundColorProperty, value);
        }

        public Color? PressedBackgroundColor
        {
            get => (Color?)GetValue(PressedBackgroundColorProperty);
            set => SetValue(PressedBackgroundColorProperty, value);
        }

        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);
            _visualElement = bindable;
            _application = Application.Current;
            if (_application is not null)
            {
                _application.RequestedThemeChanged += OnRequestedThemeChanged;
            }

            ApplyCurrentBackgroundColor(false);
            bindable.HandlerChanged += OnHandlerChanged;
            AttachPlatformLongPress(bindable);
        }

        protected override void OnDetachingFrom(VisualElement bindable)
        {
            ApplyRestingBackgroundColor(false);
            if (_application is not null)
            {
                _application.RequestedThemeChanged -= OnRequestedThemeChanged;
                _application = null;
            }

            _visualElement = null;
            bindable.HandlerChanged -= OnHandlerChanged;
            DetachPlatformLongPress();
            base.OnDetachingFrom(bindable);
        }

        private static void OnBackgroundColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LongPressBehavior behavior = (LongPressBehavior)bindable;
            behavior.ApplyCurrentBackgroundColor(false);
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            ApplyCurrentBackgroundColor(false);
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (sender is VisualElement visualElement)
            {
                AttachPlatformLongPress(visualElement);
            }
        }

        private void SetPressed(bool isPressed)
        {
            if (_isPressed == isPressed)
            {
                return;
            }

            _isPressed = isPressed;
            ApplyCurrentBackgroundColor(true);
        }

        private void ApplyCurrentBackgroundColor(bool animate)
        {
            if (_isPressed)
            {
                ApplyPressedBackgroundColor(animate);
                return;
            }

            ApplyRestingBackgroundColor(animate);
        }

        private void ApplyPressedBackgroundColor(bool animate)
        {
            if (_visualElement is not null)
            {
                Color pressedBackgroundColor = PressedBackgroundColor
                    ?? MaterialResources.GetThemeColor(
                        "M3LightPressedStateLayer",
                        "M3DarkPressedStateLayer");
                ApplyBackgroundColor(
                    pressedBackgroundColor,
                    MaterialResources.Get<int>("M3MotionPressInDuration"),
                    animate);
            }
        }

        private void ApplyRestingBackgroundColor(bool animate)
        {
            if (_visualElement is not null)
            {
                ApplyBackgroundColor(
                    RestingBackgroundColor ?? MaterialResources.Get<Color>("M3Transparent"),
                    MaterialResources.Get<int>("M3MotionPressOutDuration"),
                    animate);
            }
        }

        private void ApplyBackgroundColor(Color backgroundColor, int duration, bool animate)
        {
            VisualElement? visualElement = _visualElement;
            if (visualElement is null)
            {
                return;
            }

            MaterialMotion.UpdateBackgroundColor(
                visualElement,
                backgroundColor,
                duration,
                StateLayerAnimationName,
                animate);
        }

        private bool TryExecute(ICommand? command, object? parameter)
        {
            if (command?.CanExecute(parameter) != true)
            {
                return false;
            }

            command.Execute(parameter);
            return true;
        }

        private void AttachPlatformLongPress(VisualElement visualElement)
        {
#if ANDROID
            _platformGesture ??= new AndroidLongPressGesture(
                SetPressed,
                () => TryExecute(Command, CommandParameter),
                () => TryExecute(TapCommand, TapCommandParameter));
            _platformGesture.Attach(visualElement);
#endif
        }

        private void DetachPlatformLongPress()
        {
#if ANDROID
            _platformGesture?.Dispose();
            _platformGesture = null;
#endif
        }
    }
}
