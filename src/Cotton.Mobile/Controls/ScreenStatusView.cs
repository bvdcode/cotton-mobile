// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ScreenStatusView : ContentView
    {
        private const string DefaultTextStyleResourceKey = "M3ScreenStatus";

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

        private readonly Label _label;

        public ScreenStatusView()
        {
            _label = new Label();

            Content = _label;
            UpdateVisualState();
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenStatusView screenStatusView = (ScreenStatusView)bindable;
            screenStatusView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string textStyleResourceKey = string.IsNullOrWhiteSpace(TextStyleResourceKey)
                ? DefaultTextStyleResourceKey
                : TextStyleResourceKey;
            string text = Text ?? string.Empty;

            _label.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _label.Text = text;
            SemanticProperties.SetDescription(this, text);
        }
    }
}
