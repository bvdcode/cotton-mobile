// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class TextDocumentContentView : ContentView
    {
        private const string DefaultTextStyleResourceKey = "M3TextViewerContent";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(TextDocumentContentView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextStyleResourceKeyProperty = BindableProperty.Create(
            nameof(TextStyleResourceKey),
            typeof(string),
            typeof(TextDocumentContentView),
            DefaultTextStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Label _text;

        public TextDocumentContentView()
        {
            _text = new Label();

            ScrollView scrollView = new()
            {
                Content = _text,
            };

            Content = scrollView;
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
            TextDocumentContentView view = (TextDocumentContentView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string textStyleResourceKey = string.IsNullOrWhiteSpace(TextStyleResourceKey)
                ? DefaultTextStyleResourceKey
                : TextStyleResourceKey;

            _text.SetDynamicResource(StyleProperty, textStyleResourceKey);
            _text.Text = Text ?? string.Empty;
        }
    }
}
