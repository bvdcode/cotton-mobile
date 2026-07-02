// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class SettingsCardView : ContentView
    {
        private const string DefaultCardStyleResourceKey = "M3ContentCard";
        private const string DefaultStackStyleResourceKey = "M3SettingsSectionStack";

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

        private readonly Border _card;
        private readonly VerticalStackLayout _stack;

        public SettingsCardView()
        {
            _stack = new VerticalStackLayout();

            _card = new Border
            {
                Content = _stack,
            };

            Content = _card;
            UpdateVisualState();
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

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SettingsCardView view = (SettingsCardView)bindable;
            view.UpdateVisualState();
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
    }
}
