// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class ScreenStatusView : ContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ScreenStatusView),
            string.Empty,
            propertyChanged: OnTextChanged);

        private readonly Label _label;

        public ScreenStatusView()
        {
            _label = new Label();
            _label.SetDynamicResource(StyleProperty, "M3ScreenStatus");

            Content = _label;
            UpdateText();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenStatusView screenStatusView = (ScreenStatusView)bindable;
            screenStatusView.UpdateText();
        }

        private void UpdateText()
        {
            string text = Text ?? string.Empty;

            _label.Text = text;
            SemanticProperties.SetDescription(this, text);
        }
    }
}
