// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Views;

namespace Cotton.Mobile.Behaviors
{
    internal class AndroidLongPressGesture : IDisposable
    {
        private const int DefaultTouchSlop = 8;

        private readonly Action<bool> _setPressed;
        private readonly Func<bool> _executeLongPress;
        private readonly Func<bool> _executeTap;
        private Android.Views.View? _platformView;
        private Java.Lang.IRunnable? _longPressRunnable;
        private bool _isLongPressHandled;
        private bool _isPressed;
        private float _touchStartX;
        private float _touchStartY;
        private int _touchSlop;

        public AndroidLongPressGesture(
            Action<bool> setPressed,
            Func<bool> executeLongPress,
            Func<bool> executeTap)
        {
            _setPressed = setPressed;
            _executeLongPress = executeLongPress;
            _executeTap = executeTap;
        }

        public void Attach(VisualElement visualElement)
        {
            if (visualElement.Handler?.PlatformView is not Android.Views.View platformView
                || ReferenceEquals(_platformView, platformView))
            {
                return;
            }

            Detach();
            _platformView = platformView;
            Android.Content.Context? context = platformView.Context;
            _touchSlop = context is null
                ? DefaultTouchSlop
                : ViewConfiguration.Get(context)?.ScaledTouchSlop ?? DefaultTouchSlop;
            _platformView.Clickable = true;
            _platformView.LongClickable = true;
            _platformView.Touch += OnPlatformTouch;
        }

        public void Detach()
        {
            if (_platformView is null)
            {
                return;
            }

            CancelLongPress();
            _platformView.Touch -= OnPlatformTouch;
            _platformView = null;
        }

        public void Dispose()
        {
            Detach();
            GC.SuppressFinalize(this);
        }

        private void OnPlatformTouch(object? sender, Android.Views.View.TouchEventArgs e)
        {
            MotionEvent? motionEvent = e.Event;
            if (motionEvent is null)
            {
                return;
            }

            switch (motionEvent.ActionMasked)
            {
                case MotionEventActions.Down:
                    BeginTouch(motionEvent);
                    e.Handled = true;
                    break;
                case MotionEventActions.Move:
                    if (HasMovedPastTouchSlop(motionEvent))
                    {
                        CancelLongPress();
                    }

                    e.Handled = false;
                    break;
                case MotionEventActions.Up:
                    CompleteTouch();
                    e.Handled = true;
                    break;
                case MotionEventActions.Cancel:
                    CancelLongPress();
                    e.Handled = false;
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }

        private void BeginTouch(MotionEvent motionEvent)
        {
            CancelLongPress();
            SetPressed(true);
            _isLongPressHandled = false;
            _touchStartX = motionEvent.GetX();
            _touchStartY = motionEvent.GetY();
            _longPressRunnable = new AndroidLongPressRunnable(HandleLongPress);
            _platformView?.PostDelayed(_longPressRunnable, ViewConfiguration.LongPressTimeout);
        }

        private void CompleteTouch()
        {
            bool shouldTap = _isPressed && !_isLongPressHandled;
            CancelLongPress();
            if (shouldTap)
            {
                _executeTap();
            }
        }

        private bool HasMovedPastTouchSlop(MotionEvent motionEvent)
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

            bool didExecute = _executeLongPress();
            if (didExecute)
            {
                _platformView?.PerformHapticFeedback(FeedbackConstants.LongPress);
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

            _longPressRunnable?.Dispose();
            _longPressRunnable = null;
        }

        private void SetPressed(bool isPressed)
        {
            _isPressed = isPressed;
            _setPressed(isPressed);
        }
    }
}
