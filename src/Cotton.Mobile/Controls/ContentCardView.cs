// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(BodyContent))]
    public class ContentCardView : ContentView
    {
        private const string DefaultCardStyleResourceKey = "M3ContentCard";
        private const string CardOpacityAnimationName = "M3ContentCardOpacity";

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

        public static readonly BindableProperty IsCardVisibleProperty = BindableProperty.Create(
            nameof(IsCardVisible),
            typeof(bool),
            typeof(ContentCardView),
            true,
            propertyChanged: OnCardVisiblePropertyChanged);

        private readonly Border _card;
        private bool _hasAppliedCardVisibility;

        public ContentCardView()
        {
            _card = new Border();

            Content = _card;
            UpdateVisualState();
            UpdateCardVisibility(animateCardVisibility: false);
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

        public bool IsCardVisible
        {
            get => (bool)GetValue(IsCardVisibleProperty);
            set => SetValue(IsCardVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ContentCardView view = (ContentCardView)bindable;
            view.UpdateVisualState();
        }

        private static void OnCardVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ContentCardView view = (ContentCardView)bindable;
            view.UpdateCardVisibility(animateCardVisibility: true);
        }

        private void UpdateVisualState()
        {
            string cardStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                CardStyleResourceKey,
                DefaultCardStyleResourceKey);

            _card.SetDynamicResource(StyleProperty, cardStyleResourceKey);

            if (_card.Content != BodyContent)
            {
                _card.Content = BodyContent;
            }
        }

        private void UpdateCardVisibility(bool animateCardVisibility)
        {
            bool shouldAnimate = animateCardVisibility && _hasAppliedCardVisibility;
            MaterialVisibility.Update(
                this,
                IsCardVisible,
                CardOpacityAnimationName,
                shouldAnimate,
                UpdateInputTransparency);
            _hasAppliedCardVisibility = true;
        }

        private void UpdateInputTransparency()
        {
            InputTransparent = !IsCardVisible;
        }
    }
}
