// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ScreenStatusView : ContentView
    {
        private const string DefaultTextStyleResourceKey = "M3ScreenStatus";
        private const string StatusOpacityAnimationName = "M3ScreenStatusOpacity";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ScreenStatusView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStyleResourceKey),
            typeof(string),
            typeof(ScreenStatusView),
            DefaultTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsStatusVisibleProperty = BindableProperty.Create(
            nameof(IsStatusVisible),
            typeof(bool),
            typeof(ScreenStatusView),
            true,
            propertyChanged: OnStatusVisiblePropertyChanged);

        private readonly Label _label;
        private bool _hasAppliedStatusVisibility;

        public ScreenStatusView()
        {
            InputTransparent = true;
            _label = new Label();

            Content = _label;
            UpdateVisualState(animateStatusVisibility: false);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string TextStyleResourceKey
        {
            get => (string)GetValue(TextStyleResourceKeyProperty);
            set => SetValue(TextStyleResourceKeyProperty, value);
        }

        public bool IsStatusVisible
        {
            get => (bool)GetValue(IsStatusVisibleProperty);
            set => SetValue(IsStatusVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenStatusView screenStatusView = (ScreenStatusView)bindable;
            screenStatusView.UpdateVisualState(animateStatusVisibility: false);
        }

        private static void OnStatusVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenStatusView screenStatusView = (ScreenStatusView)bindable;
            screenStatusView.UpdateVisualState(animateStatusVisibility: true);
        }

        private void UpdateVisualState(bool animateStatusVisibility)
        {
            string textStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                TextStyleResourceKey,
                DefaultTextStyleResourceKey);
            string text = Text ?? string.Empty;

            _label.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _label.Text = text;
            SemanticProperties.SetDescription(this, text);
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
