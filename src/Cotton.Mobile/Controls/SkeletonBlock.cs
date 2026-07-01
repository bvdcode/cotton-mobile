// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class SkeletonBlock : Border
    {
        private const string PulseAnimationName = nameof(SkeletonBlock) + "Pulse";

        public static readonly BindableProperty IdleOpacityProperty = BindableProperty.Create(
            nameof(IdleOpacity),
            typeof(double),
            typeof(SkeletonBlock),
            propertyChanged: OnPulsePropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SkeletonIdleOpacity"));

        public static readonly BindableProperty PulseOpacityProperty = BindableProperty.Create(
            nameof(PulseOpacity),
            typeof(double),
            typeof(SkeletonBlock),
            propertyChanged: OnPulsePropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3SkeletonPulseOpacity"));

        public static readonly BindableProperty PulseDurationProperty = BindableProperty.Create(
            nameof(PulseDuration),
            typeof(int),
            typeof(SkeletonBlock),
            propertyChanged: OnPulsePropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<int>("M3MotionSkeletonPulseDuration"));

        private bool _isLoaded;
        private bool _isPulsing;

        public SkeletonBlock()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            InputTransparent = true;
            Opacity = IdleOpacity;
        }

        public double IdleOpacity
        {
            get => (double)GetValue(IdleOpacityProperty);
            set => SetValue(IdleOpacityProperty, value);
        }

        public double PulseOpacity
        {
            get => (double)GetValue(PulseOpacityProperty);
            set => SetValue(PulseOpacityProperty, value);
        }

        public int PulseDuration
        {
            get => (int)GetValue(PulseDurationProperty);
            set => SetValue(PulseDurationProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsVisible), StringComparison.Ordinal))
            {
                UpdatePulseState();
            }
        }

        private static void OnPulsePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SkeletonBlock skeletonBlock = (SkeletonBlock)bindable;
            skeletonBlock.RestartPulse();
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            _isLoaded = true;
            UpdatePulseState();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            _isLoaded = false;
            StopPulse();
        }

        private void RestartPulse()
        {
            StopPulse();
            UpdatePulseState();
        }

        private void UpdatePulseState()
        {
            if (ShouldPulse())
            {
                StartPulse();
                return;
            }

            StopPulse();
        }

        private void StartPulse()
        {
            if (_isPulsing)
            {
                return;
            }

            int pulseDuration = PulseDuration;
            if (pulseDuration <= 0)
            {
                Opacity = PulseOpacity;
                return;
            }

            _isPulsing = true;
            Opacity = IdleOpacity;

            Animation pulse = new();
            pulse.Add(
                0,
                0.5,
                new Animation(
                    value => Opacity = value,
                    IdleOpacity,
                    PulseOpacity,
                    Easing.CubicInOut));
            pulse.Add(
                0.5,
                1,
                new Animation(
                    value => Opacity = value,
                    PulseOpacity,
                    IdleOpacity,
                    Easing.CubicInOut));

            pulse.Commit(
                this,
                PulseAnimationName,
                length: MaterialMotion.Duration(pulseDuration),
                repeat: ShouldPulse);
        }

        private void StopPulse()
        {
            if (_isPulsing)
            {
                this.AbortAnimation(PulseAnimationName);
                _isPulsing = false;
            }

            Opacity = IdleOpacity;
        }

        private bool ShouldPulse()
        {
            return _isLoaded && IsVisible;
        }
    }
}
