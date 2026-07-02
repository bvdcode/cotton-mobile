// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(BodyContent))]
    public class ContentCardView : ContentView
    {
        private const string DefaultCardStyleResourceKey = "M3ContentCard";

        public static readonly BindableProperty CardStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CardStyleResourceKey),
            typeof(string),
            typeof(ContentCardView),
            DefaultCardStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(
            nameof(BodyContent),
            typeof(View),
            typeof(ContentCardView),
            default(View),
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _card;

        public ContentCardView()
        {
            _card = new Border();

            Content = _card;
            UpdateVisualState();
        }

        public string CardStyleResourceKey
        {
            get => (string)GetValue(CardStyleResourceKeyProperty);
            set => SetValue(CardStyleResourceKeyProperty, value);
        }

        public View? BodyContent
        {
            get => (View?)GetValue(BodyContentProperty);
            set => SetValue(BodyContentProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ContentCardView view = (ContentCardView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string cardStyleResourceKey = string.IsNullOrWhiteSpace(CardStyleResourceKey)
                ? DefaultCardStyleResourceKey
                : CardStyleResourceKey;

            _card.SetDynamicResource(StyleProperty, cardStyleResourceKey);

            if (_card.Content != BodyContent)
            {
                _card.Content = BodyContent;
            }
        }
    }
}
