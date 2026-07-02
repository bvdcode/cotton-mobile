// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class StackedContentView : ContentView
    {
        private const string DefaultStackStyleResourceKey = "M3SettingsSectionStack";

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(StackedContentView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly VerticalStackLayout _stack;

        public StackedContentView()
        {
            _stack = new VerticalStackLayout();

            Content = _stack;
            UpdateVisualState();
        }

        public IList<IView> Items => _stack.Children;

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            StackedContentView view = (StackedContentView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string stackStyleResourceKey = string.IsNullOrWhiteSpace(StackStyleResourceKey)
                ? DefaultStackStyleResourceKey
                : StackStyleResourceKey;

            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
        }
    }
}
