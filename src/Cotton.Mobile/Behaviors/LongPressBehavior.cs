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
        private bool _isPressed;

#if ANDROID
        private Android.Views.View? _platformView;
        private Java.Lang.IRunnable? _longPressRunnable;
        private bool _isLongPressHandled;
        private float _touchStartX;
        private float _touchStartY;
        private int _touchSlop;
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
            ApplyCurrentBackgroundColor(false);
            bindable.HandlerChanged += OnHandlerChanged;
            AttachPlatformLongPress(bindable);
        }

        protected override void OnDetachingFrom(VisualElement bindable)
        {
            ApplyRestingBackgroundColor(false);
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

            if (animate)
            {
                MaterialMotion.AnimateBackgroundColor(
                    visualElement,
                    backgroundColor,
                    duration,
                    StateLayerAnimationName);
                return;
            }

            MaterialMotion.SetBackgroundColor(visualElement, backgroundColor, StateLayerAnimationName);
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
            if (visualElement.Handler?.PlatformView is not Android.Views.View platformView
                || ReferenceEquals(_platformView, platformView))
            {
                return;
            }

            DetachPlatformLongPress();
            _platformView = platformView;
            Android.Content.Context? context = platformView.Context;
            _touchSlop = context is null
                ? 8
                : Android.Views.ViewConfiguration.Get(context)?.ScaledTouchSlop ?? 8;
            _platformView.Clickable = true;
            _platformView.LongClickable = true;
            _platformView.Touch += OnPlatformTouch;
#endif
        }

        private void DetachPlatformLongPress()
        {
#if ANDROID
            if (_platformView is null)
            {
                return;
            }

            CancelLongPress();
            _platformView.Touch -= OnPlatformTouch;
            _platformView = null;
#endif
        }

#if ANDROID
        private void OnPlatformTouch(object? sender, Android.Views.View.TouchEventArgs e)
        {
            Android.Views.MotionEvent? motionEvent = e.Event;
            if (motionEvent is null)
            {
                return;
            }

            switch (motionEvent.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    BeginTouch(motionEvent);
                    e.Handled = true;
                    break;
                case Android.Views.MotionEventActions.Move:
                    if (HasMovedPastTouchSlop(motionEvent))
                    {
                        CancelLongPress();
                    }

                    e.Handled = false;
                    break;
                case Android.Views.MotionEventActions.Up:
                    CompleteTouch();
                    e.Handled = true;
                    break;
                case Android.Views.MotionEventActions.Cancel:
                    CancelLongPress();
                    e.Handled = false;
                    break;
            }
        }

        private void BeginTouch(Android.Views.MotionEvent motionEvent)
        {
            CancelLongPress();
            SetPressed(true);
            _isLongPressHandled = false;
            _touchStartX = motionEvent.GetX();
            _touchStartY = motionEvent.GetY();
            _longPressRunnable = new LongPressRunnable(HandleLongPress);
            _platformView?.PostDelayed(_longPressRunnable, Android.Views.ViewConfiguration.LongPressTimeout);
        }

        private void CompleteTouch()
        {
            bool shouldTap = _isPressed && !_isLongPressHandled;
            CancelLongPress();
            if (shouldTap)
            {
                TryExecute(TapCommand, TapCommandParameter);
            }
        }

        private bool HasMovedPastTouchSlop(Android.Views.MotionEvent motionEvent)
        {
            float deltaX = motionEvent.GetX() - _touchStartX;
            float deltaY = motionEvent.GetY() - _touchStartY;
            return (deltaX * deltaX) + (deltaY * deltaY) > _touchSlop * _touchSlop;
        }

        private void HandleLongPress()
        {
            if (!_isPressed || _isLongPressHandled)
            {
                return;
            }

            bool didExecute = TryExecute(Command, CommandParameter);
            if (didExecute)
            {
                _platformView?.PerformHapticFeedback(Android.Views.FeedbackConstants.LongPress);
            }

            _isLongPressHandled = didExecute;
        }

        private void CancelLongPress()
        {
            SetPressed(false);
            if (_platformView is not null && _longPressRunnable is not null)
            {
                _platformView.RemoveCallbacks(_longPressRunnable);
            }

            _longPressRunnable = null;
        }

        private class LongPressRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            private readonly Action _action;

            public LongPressRunnable(Action action)
            {
                ArgumentNullException.ThrowIfNull(action);
                _action = action;
            }

            public void Run()
            {
                _action();
            }
        }
#endif
    }
}
