// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public abstract class MaterialAnimatedContentView : ContentView
    {
        private const string AppearanceAnimationName = nameof(MaterialAnimatedContentView) + "Appearance";

        public static readonly BindableProperty AppearanceDurationProperty = BindableProperty.Create(
            nameof(AppearanceDuration),
            typeof(int),
            typeof(MaterialAnimatedContentView),
            propertyChanged: OnAppearancePropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<int>("M3MotionContentEnterDuration"));

        private bool _isLoaded;

        protected MaterialAnimatedContentView()
        {
            Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public int AppearanceDuration
        {
            get => (int)GetValue(AppearanceDurationProperty);
            set => SetValue(AppearanceDurationProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsVisible), StringComparison.Ordinal))
            {
                UpdateAppearanceState();
            }
        }

        private static void OnAppearancePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MaterialAnimatedContentView view = (MaterialAnimatedContentView)bindable;
            view.UpdateAppearanceState();
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            _isLoaded = true;
            UpdateAppearanceState();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            _isLoaded = false;
            this.AbortAnimation(AppearanceAnimationName);
            Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
        }

        private void UpdateAppearanceState()
        {
            this.AbortAnimation(AppearanceAnimationName);

            if (!_isLoaded || !IsVisible)
            {
                Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
                return;
            }

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                MaterialMotion.Value("M3MotionVisibleOpacity"),
                AppearanceDuration,
                AppearanceAnimationName,
                true,
                opacity => Opacity = opacity,
                finished: null);
        }
    }
}
