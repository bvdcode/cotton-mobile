// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class LoadingIndicatorView : ContentView
    {
        private const string DefaultFrameStyleResourceKey = "M3LoadingIndicatorFrame";
        private const string DefaultIndicatorStyleResourceKey = "M3LoadingActivityIndicator";

        public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
            nameof(IsRunning),
            typeof(bool),
            typeof(LoadingIndicatorView),
            false,
            propertyChanged: OnIsRunningChanged);

        public static readonly BindableProperty FrameStyleResourceKeyProperty = BindableProperty.Create(
            nameof(FrameStyleResourceKey),
            typeof(string),
            typeof(LoadingIndicatorView),
            DefaultFrameStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IndicatorStyleResourceKeyProperty = BindableProperty.Create(
            nameof(IndicatorStyleResourceKey),
            typeof(string),
            typeof(LoadingIndicatorView),
            DefaultIndicatorStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _frame;
        private readonly ActivityIndicator _indicator;

        public LoadingIndicatorView()
        {
            _indicator = new ActivityIndicator();
            _frame = new Border
            {
                Content = _indicator,
            };

            Content = _frame;
            InputTransparent = true;
            UpdateVisualState();
        }

        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            set => SetValue(IsRunningProperty, value);
        }

        public string FrameStyleResourceKey
        {
            get => (string)GetValue(FrameStyleResourceKeyProperty);
            set => SetValue(FrameStyleResourceKeyProperty, value);
        }

        public string IndicatorStyleResourceKey
        {
            get => (string)GetValue(IndicatorStyleResourceKeyProperty);
            set => SetValue(IndicatorStyleResourceKeyProperty, value);
        }

        private static void OnIsRunningChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LoadingIndicatorView loadingIndicatorView = (LoadingIndicatorView)bindable;
            loadingIndicatorView.UpdateVisualState();
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LoadingIndicatorView loadingIndicatorView = (LoadingIndicatorView)bindable;
            loadingIndicatorView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string frameStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                FrameStyleResourceKey,
                DefaultFrameStyleResourceKey);
            string indicatorStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                IndicatorStyleResourceKey,
                DefaultIndicatorStyleResourceKey);

            _frame.SetDynamicResource(StyleProperty, frameStyleResourceKey);
            _indicator.SetDynamicResource(StyleProperty, indicatorStyleResourceKey);
            _indicator.IsRunning = IsRunning;
        }
    }
}
