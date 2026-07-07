// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class SettingsCardView : ContentView
    {
        private const string DefaultCardStyleResourceKey = "M3ContentCard";
        private const string DefaultStackStyleResourceKey = "M3SettingsSectionStack";
        private const string CardOpacityAnimationName = "M3SettingsCardOpacity";

        public static readonly BindableProperty CardStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CardStyleResourceKey),
            typeof(string),
            typeof(SettingsCardView),
            DefaultCardStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(SettingsCardView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsCardVisibleProperty = BindableProperty.Create(
            nameof(IsCardVisible),
            typeof(bool),
            typeof(SettingsCardView),
            true,
            propertyChanged: OnCardVisiblePropertyChanged);

        private readonly Border _card;
        private readonly VerticalStackLayout _stack;
        private bool _hasAppliedCardVisibility;

        public SettingsCardView()
        {
            _stack = new VerticalStackLayout();

            _card = new Border
            {
                Content = _stack,
            };

            Content = _card;
            UpdateVisualState();
            UpdateCardVisibility(animateCardVisibility: false);
        }

        public IList<IView> Items => _stack.Children;

        public string CardStyleResourceKey
        {
            get => (string)GetValue(CardStyleResourceKeyProperty);
            set => SetValue(CardStyleResourceKeyProperty, value);
        }

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        public bool IsCardVisible
        {
            get => (bool)GetValue(IsCardVisibleProperty);
            set => SetValue(IsCardVisibleProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsCardView view = (SettingsCardView)bindable;
            view.UpdateVisualState();
        }

        private static void OnCardVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsCardView view = (SettingsCardView)bindable;
            view.UpdateCardVisibility(animateCardVisibility: true);
        }

        private void UpdateVisualState()
        {
            string cardStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                CardStyleResourceKey,
                DefaultCardStyleResourceKey);
            string stackStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                StackStyleResourceKey,
                DefaultStackStyleResourceKey);

            _card.SetDynamicResource(StyleProperty, cardStyleResourceKey);
            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
        }

        private void UpdateCardVisibility(bool animateCardVisibility)
        {
            bool isCardVisible = IsCardVisible;
            bool shouldAnimate = animateCardVisibility && _hasAppliedCardVisibility;
            double targetOpacity = isCardVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            if (isCardVisible)
            {
                IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                CardOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteCardVisibility);
            _hasAppliedCardVisibility = true;
        }

        private void CompleteCardVisibility()
        {
            IsVisible = IsCardVisible;
        }
    }
}
