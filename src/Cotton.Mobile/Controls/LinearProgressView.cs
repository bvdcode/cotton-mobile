// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class LinearProgressView : ContentView
    {
        private const string DefaultProgressStyleResourceKey = "M3LinearProgressBar";

        public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
            nameof(Progress),
            typeof(double),
            typeof(LinearProgressView),
            0d,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ProgressStyleResourceKeyProperty = BindableProperty.Create(
            nameof(ProgressStyleResourceKey),
            typeof(string),
            typeof(LinearProgressView),
            DefaultProgressStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly ProgressBar _progressBar;

        public LinearProgressView()
        {
            InputTransparent = true;

            _progressBar = new ProgressBar();
            Content = _progressBar;
            UpdateVisualState();
        }

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public string ProgressStyleResourceKey
        {
            get => (string)GetValue(ProgressStyleResourceKeyProperty);
            set => SetValue(ProgressStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LinearProgressView view = (LinearProgressView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string progressStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                ProgressStyleResourceKey,
                DefaultProgressStyleResourceKey);

            _progressBar.SetDynamicResource(StyleProperty, progressStyleResourceKey);
            _progressBar.Progress = Progress;
        }
    }
}
