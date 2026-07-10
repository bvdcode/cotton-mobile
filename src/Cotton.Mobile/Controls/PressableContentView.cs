// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public abstract class PressableContentView : MaterialThemeContentView
    {
        public static readonly BindableProperty PressedScaleProperty = BindableProperty.Create(
            nameof(PressedScale),
            typeof(double),
            typeof(PressableContentView),
            propertyChanged: OnInteractionMetricChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3InteractionRestScale"));

        public static readonly BindableProperty PressInDurationProperty = BindableProperty.Create(
            nameof(PressInDuration),
            typeof(int),
            typeof(PressableContentView),
            defaultValueCreator: _ => MaterialResources.Get<int>("M3MotionPressInDuration"));

        public static readonly BindableProperty PressOutDurationProperty = BindableProperty.Create(
            nameof(PressOutDuration),
            typeof(int),
            typeof(PressableContentView),
            defaultValueCreator: _ => MaterialResources.Get<int>("M3MotionPressOutDuration"));

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
        private int _touchSlop;
#endif

        protected PressableContentView()
        {
#if ANDROID
            HandlerChanged += OnAndroidHandlerChanged;
#else
            TapGestureRecognizer tap = new();
            tap.Tapped += HandleTapped;
            GestureRecognizers.Add(tap);
#endif
        }

        protected bool IsPressed { get; private set; }

        public double PressedScale
        {
            get => (double)GetValue(PressedScaleProperty);
            set => SetValue(PressedScaleProperty, value);
        }

        public int PressInDuration
        {
            get => (int)GetValue(PressInDurationProperty);
            set => SetValue(PressInDurationProperty, value);
        }

        public int PressOutDuration
        {
            get => (int)GetValue(PressOutDurationProperty);
            set => SetValue(PressOutDurationProperty, value);
        }

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

        protected void UpdatePlatformPressability()
        {
#if ANDROID
            if (_platformView is null)
            {
                return;
            }

            bool canHandlePress = CanHandlePress();
            _platformView.Enabled = canHandlePress;
            _platformView.Clickable = canHandlePress;
#endif
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

            if (!string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal))
            {
                return;
            }

            if (IsPressed)
            {
                SetPressed(false);
            }

            UpdatePlatformPressability();
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
            AnimatePressedScale();
            OnPressedStateChanged();
        }

        private void AnimatePressedScale()
        {
            double targetScale = IsPressed ? PressedScale : MaterialMotion.Value("M3InteractionRestScale");
            if (Scale == targetScale)
            {
                return;
            }

            int duration = IsPressed ? PressInDuration : PressOutDuration;
            if (duration <= 0)
            {
                Scale = targetScale;
                return;
            }

            this.AbortAnimation(nameof(PressableContentView));
            _ = this.ScaleToAsync(targetScale, MaterialMotion.Duration(duration), Easing.CubicOut);
        }

#if ANDROID
        private void OnAndroidHandlerChanged(object? sender, EventArgs e)
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
            Android.Content.Context? context = platformView.Context;
            _touchSlop = context is null
                ? 8
                : Android.Views.ViewConfiguration.Get(context)?.ScaledTouchSlop ?? 8;
            _platformView.Touch += OnPlatformTouch;
            UpdatePlatformPressability();
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
                case Android.Views.MotionEventActions.Move:
                    SetPressed(CanHandlePress() && IsTouchInsideBounds(motionEvent));
                    e.Handled = false;
                    break;
                case Android.Views.MotionEventActions.Up:
                    bool shouldExecute = IsPressed && CanHandlePress() && IsTouchInsideBounds(motionEvent);
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

        private bool IsTouchInsideBounds(Android.Views.MotionEvent motionEvent)
        {
            Android.Views.View? platformView = _platformView;
            if (platformView is null)
            {
                return false;
            }

            float x = motionEvent.GetX();
            float y = motionEvent.GetY();
            return x >= -_touchSlop
                && y >= -_touchSlop
                && x <= platformView.Width + _touchSlop
                && y <= platformView.Height + _touchSlop;
        }
#endif
    }
}
