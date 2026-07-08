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

        public static readonly BindableProperty IsContentVisibleProperty = BindableProperty.Create(
            nameof(IsContentVisible),
            typeof(bool),
            typeof(MaterialAnimatedContentView),
            true,
            propertyChanged: OnContentVisiblePropertyChanged);

        private bool _hasAppliedContentVisibility;
        private bool _isLoaded;

        protected MaterialAnimatedContentView()
        {
            Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
            UpdateInputTransparency();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public int AppearanceDuration
        {
            get => (int)GetValue(AppearanceDurationProperty);
            set => SetValue(AppearanceDurationProperty, value);
        }

        public bool IsContentVisible
        {
            get => (bool)GetValue(IsContentVisibleProperty);
            set => SetValue(IsContentVisibleProperty, value);
        }

        protected virtual bool IsContentInteractiveWhenVisible => true;

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

        private static void OnContentVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MaterialAnimatedContentView view = (MaterialAnimatedContentView)bindable;
            view.UpdateContentVisibility(animateContentVisibility: true);
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
            UpdateInputTransparency();

            if (!_isLoaded || !IsVisible || !IsContentVisible)
            {
                Opacity = MaterialMotion.Value("M3MotionHiddenOpacity");
                _hasAppliedContentVisibility = true;
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
            _hasAppliedContentVisibility = true;
        }

        private void UpdateContentVisibility(bool animateContentVisibility)
        {
            bool isContentVisible = IsContentVisible;
            bool shouldAnimate = animateContentVisibility && _hasAppliedContentVisibility && _isLoaded;
            double targetOpacity = isContentVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");

            if (isContentVisible)
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
                AppearanceDuration,
                AppearanceAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteContentVisibility);
            _hasAppliedContentVisibility = true;
        }

        private void CompleteContentVisibility()
        {
            IsVisible = IsContentVisible;
            UpdateInputTransparency();
        }

        private void UpdateInputTransparency()
        {
            InputTransparent = !IsVisible || !IsContentVisible || !IsContentInteractiveWhenVisible;
        }
    }
}
