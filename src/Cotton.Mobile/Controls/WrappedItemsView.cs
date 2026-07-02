// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Collections;

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(ItemTemplate))]
    public class WrappedItemsView : ContentView
    {
        private const string DefaultLayoutStyleResourceKey = "M3FileTileWrapLayout";

        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(WrappedItemsView),
            null,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(WrappedItemsView),
            null,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty LayoutStyleResourceKeyProperty = BindableProperty.Create(
            nameof(LayoutStyleResourceKey),
            typeof(string),
            typeof(WrappedItemsView),
            DefaultLayoutStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly FlexLayout _layout;

        public WrappedItemsView()
        {
            _layout = new FlexLayout();

            Content = _layout;
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

        public string LayoutStyleResourceKey
        {
            get => (string)GetValue(LayoutStyleResourceKeyProperty);
            set => SetValue(LayoutStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            WrappedItemsView view = (WrappedItemsView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string layoutStyleResourceKey = string.IsNullOrWhiteSpace(LayoutStyleResourceKey)
                ? DefaultLayoutStyleResourceKey
                : LayoutStyleResourceKey;

            _layout.SetDynamicResource(StyleProperty, layoutStyleResourceKey);
            BindableLayout.SetItemsSource(_layout, ItemsSource);
            BindableLayout.SetItemTemplate(_layout, ItemTemplate);
        }
    }
}
