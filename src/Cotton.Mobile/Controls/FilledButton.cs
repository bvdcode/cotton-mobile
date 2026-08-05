// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class FilledButton : CommandPressableContentView
    {
        private const string OpacityAnimationName = "M3FilledButtonOpacity";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(FilledButton),
            string.Empty,
            propertyChanged: OnTextChanged);

        private readonly FilledButtonVisual _visual;
        private bool _hasAppliedVisualState;

        public FilledButton()
        {
            _visual = new FilledButtonVisual();
            Content = _visual;
            UpdateVisualState(false);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(MinimumHeightRequest), StringComparison.Ordinal)
                || string.Equals(propertyName, nameof(MinimumWidthRequest), StringComparison.Ordinal))
            {
                UpdateVisualState(true);
            }
        }

        protected override void OnPressedStateChanged()
        {
            UpdateVisualState(true);
        }

        protected override void OnCommandStateChanged()
        {
            UpdateVisualState(true);
        }

        protected override void OnRequestedThemeChanged(AppThemeChangedEventArgs e)
        {
            UpdateVisualState(false);
        }

        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FilledButton button = (FilledButton)bindable;
            button.UpdateVisualState(true);
        }

        private void UpdateVisualState(bool animateState)
        {
            if (_visual is null)
            {
                return;
            }

            bool canPress = CanHandlePress();
            int duration = IsPressed ? PressInDuration : PressOutDuration;
            bool shouldAnimate = animateState && _hasAppliedVisualState;
            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                ResolvePressableOpacity(1),
                duration,
                OpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity);
            _visual.UpdateVisualState(
                Text,
                IsPressed,
                canPress,
                MinimumHeightRequest,
                MinimumWidthRequest,
                duration,
                shouldAnimate);
            _hasAppliedVisualState = true;
        }
    }
}
