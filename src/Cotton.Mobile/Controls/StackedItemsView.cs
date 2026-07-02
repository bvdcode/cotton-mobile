// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Collections;

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(ItemTemplate))]
    public class StackedItemsView : MaterialAnimatedContentView
    {
        private const string DefaultStackStyleResourceKey = "M3SettingsSectionStack";

        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(StackedItemsView),
            null,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(StackedItemsView),
            null,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty StackStyleResourceKeyProperty = BindableProperty.Create(
            nameof(StackStyleResourceKey),
            typeof(string),
            typeof(StackedItemsView),
            DefaultStackStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly VerticalStackLayout _stack;

        public StackedItemsView()
        {
            _stack = new VerticalStackLayout();

            Content = _stack;
            UpdateVisualState();
        }

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public DataTemplate? ItemTemplate
        {
            get => (DataTemplate?)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public string StackStyleResourceKey
        {
            get => (string)GetValue(StackStyleResourceKeyProperty);
            set => SetValue(StackStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            StackedItemsView view = (StackedItemsView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string stackStyleResourceKey = string.IsNullOrWhiteSpace(StackStyleResourceKey)
                ? DefaultStackStyleResourceKey
                : StackStyleResourceKey;

            _stack.SetDynamicResource(StyleProperty, stackStyleResourceKey);
            BindableLayout.SetItemsSource(_stack, ItemsSource);
            BindableLayout.SetItemTemplate(_stack, ItemTemplate);
        }
    }
}
