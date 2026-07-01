// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public abstract class PressableContentView : ContentView
    {
        public static readonly BindableProperty PressedOpacityMultiplierProperty = BindableProperty.Create(
            nameof(PressedOpacityMultiplier),
            typeof(double),
            typeof(PressableContentView),
            propertyChanged: OnInteractionMetricChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3InteractionPressedOpacityFactor"));

        public static readonly BindableProperty DisabledOpacityProperty = BindableProperty.Create(
            nameof(DisabledOpacity),
            typeof(double),
            typeof(PressableContentView),
            propertyChanged: OnInteractionMetricChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3InteractionDisabledOpacity"));

#if ANDROID
        private Android.Views.View? _platformView;
#endif

        protected PressableContentView()
        {
#if ANDROID
            HandlerChanged += OnHandlerChanged;
#else
            TapGestureRecognizer tap = new();
            tap.Tapped += HandleTapped;
            GestureRecognizers.Add(tap);
#endif
        }

        protected bool IsPressed { get; private set; }

        public double PressedOpacityMultiplier
        {
            get => (double)GetValue(PressedOpacityMultiplierProperty);
            set => SetValue(PressedOpacityMultiplierProperty, value);
        }

        public double DisabledOpacity
        {
            get => (double)GetValue(DisabledOpacityProperty);
            set => SetValue(DisabledOpacityProperty, value);
        }

        protected double ResolvePressableOpacity(double enabledOpacity)
        {
            if (!CanHandlePress())
            {
                return DisabledOpacity;
            }

            return IsPressed ? enabledOpacity * PressedOpacityMultiplier : enabledOpacity;
        }

        protected virtual bool CanHandlePress()
        {
            return IsEnabled;
        }

        protected virtual void OnPressedStateChanged()
        {
        }

        protected abstract void ExecutePress();

        private static void OnInteractionMetricChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is PressableContentView pressableContentView)
            {
                pressableContentView.OnPressedStateChanged();
            }
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal) && IsPressed)
            {
                SetPressed(false);
            }
        }

        private void HandleTapped(object? sender, TappedEventArgs e)
        {
            if (CanHandlePress())
            {
                ExecutePress();
            }
        }

        private void SetPressed(bool isPressed)
        {
            if (IsPressed == isPressed)
            {
                return;
            }

            IsPressed = isPressed;
            OnPressedStateChanged();
        }

#if ANDROID
        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            if (Handler?.PlatformView is not Android.Views.View platformView)
            {
                DetachPlatformTouch();
                return;
            }

            if (ReferenceEquals(_platformView, platformView))
            {
                return;
            }

            DetachPlatformTouch();
            _platformView = platformView;
            _platformView.Clickable = true;
            _platformView.Touch += OnPlatformTouch;
        }

        private void DetachPlatformTouch()
        {
            if (_platformView is null)
            {
                return;
            }

            _platformView.Touch -= OnPlatformTouch;
            _platformView = null;
        }

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
                    if (CanHandlePress())
                    {
                        SetPressed(true);
                    }

                    e.Handled = true;
                    break;
                case Android.Views.MotionEventActions.Up:
                    bool shouldExecute = IsPressed && CanHandlePress();
                    SetPressed(false);
                    if (shouldExecute)
                    {
                        ExecutePress();
                    }

                    e.Handled = true;
                    break;
                case Android.Views.MotionEventActions.Cancel:
                    SetPressed(false);
                    e.Handled = false;
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }
#endif
    }
}
