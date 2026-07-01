// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class LoadingIndicatorView : ContentView
    {
        public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
            nameof(IsRunning),
            typeof(bool),
            typeof(LoadingIndicatorView),
            false,
            propertyChanged: OnIsRunningChanged);

        private readonly ActivityIndicator _indicator;

        public LoadingIndicatorView()
        {
            _indicator = new ActivityIndicator();
            _indicator.SetDynamicResource(StyleProperty, "M3LoadingActivityIndicator");

            Border frame = new()
            {
                Content = _indicator,
            };
            frame.SetDynamicResource(StyleProperty, "M3LoadingIndicatorFrame");

            Content = frame;
            InputTransparent = true;
            UpdateVisualState();
        }

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            set => SetValue(IsRunningProperty, value);
        }

        private static void OnIsRunningChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LoadingIndicatorView loadingIndicatorView = (LoadingIndicatorView)bindable;
            loadingIndicatorView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            _indicator.IsRunning = IsRunning;
        }
    }
}
