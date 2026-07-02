// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ViewerStatusOverlayView : ContentView
    {
        private const string DefaultStatusStyleResourceKey = "M3ViewerOverlayStatus";

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

        private readonly Label _status;

        public ViewerStatusOverlayView()
        {
            _status = new Label();

            Content = _status;
            UpdateVisualState();
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ViewerStatusOverlayView view = (ViewerStatusOverlayView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string statusStyleResourceKey = string.IsNullOrWhiteSpace(StatusStyleResourceKey)
                ? DefaultStatusStyleResourceKey
                : StatusStyleResourceKey;

            _status.SetDynamicResource(StyleProperty, statusStyleResourceKey);
            _status.Text = Text ?? string.Empty;
        }
    }
}
