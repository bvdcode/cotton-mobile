// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ViewerStatusOverlayView : ContentView
    {
        private const string DefaultStatusStyleResourceKey = "M3ViewerOverlayStatus";
        private const string StatusOpacityAnimationName = "M3ViewerOverlayStatusOpacity";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ViewerStatusOverlayView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StatusStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StatusStyleResourceKey),
            typeof(string),
            typeof(ViewerStatusOverlayView),
            DefaultStatusStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(ViewerStatusOverlayView),
            true,
            propertyChanged: OnStatusVisiblePropertyChanged);

        private readonly Label _status;
        private bool _hasAppliedStatusVisibility;

        public ViewerStatusOverlayView()
        {
            _status = new Label();

            Content = _status;
            UpdateVisualState(animateStatusVisibility: false);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string StatusStyleResourceKey
        {
            get => (string)GetValue(StatusStyleResourceKeyProperty);
            set => SetValue(StatusStyleResourceKeyProperty, value);
        }

        public bool IsStatusVisible
        {
            get => (bool)GetValue(IsStatusVisibleProperty);
            set => SetValue(IsStatusVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerStatusOverlayView view = (ViewerStatusOverlayView)bindable;
            view.UpdateVisualState(animateStatusVisibility: false);
        }

        private static void OnStatusVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerStatusOverlayView view = (ViewerStatusOverlayView)bindable;
            view.UpdateVisualState(animateStatusVisibility: true);
        }

        private void UpdateVisualState(bool animateStatusVisibility)
        {
            string statusStyleResourceKey = string.IsNullOrWhiteSpace(StatusStyleResourceKey)
                ? DefaultStatusStyleResourceKey
                : StatusStyleResourceKey;

            _status.SetDynamicResource(StyleProperty, statusStyleResourceKey);
            _status.Text = Text ?? string.Empty;
            UpdateStatusVisibility(animateStatusVisibility);
        }

        private void UpdateStatusVisibility(bool animateStatusVisibility)
        {
            bool isStatusVisible = IsStatusVisible;
            bool shouldAnimate = animateStatusVisibility && _hasAppliedStatusVisibility;
            double targetOpacity = isStatusVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isStatusVisible)
            {
                IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                StatusOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteStatusVisibility);
            _hasAppliedStatusVisibility = true;
        }

        private void CompleteStatusVisibility()
        {
            IsVisible = IsStatusVisible;
        }
    }
}
