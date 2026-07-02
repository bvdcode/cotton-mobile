// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Collections;

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(ItemTemplate))]
    public class MaterialCollectionView : ContentView
    {
        private const string DefaultCollectionStyleResourceKey = "M3MaterialCollectionView";

        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(MaterialCollectionView),
            null,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(MaterialCollectionView),
            null,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ItemsLayoutProperty = BindableProperty.Create(
            nameof(ItemsLayout),
            typeof(IItemsLayout),
            typeof(MaterialCollectionView),
            LinearItemsLayout.Vertical,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ItemSizingStrategyProperty = BindableProperty.Create(
            nameof(ItemSizingStrategy),
            typeof(ItemSizingStrategy),
            typeof(MaterialCollectionView),
            ItemSizingStrategy.MeasureFirstItem,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SelectionModeProperty = BindableProperty.Create(
            nameof(SelectionMode),
            typeof(SelectionMode),
            typeof(MaterialCollectionView),
            SelectionMode.None,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CollectionStyleResourceKeyProperty = BindableProperty.Create(
            nameof(CollectionStyleResourceKey),
            typeof(string),
            typeof(MaterialCollectionView),
            DefaultCollectionStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly CollectionView _collection;

        public MaterialCollectionView()
        {
            _collection = new CollectionView();

            Content = _collection;
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

        public IItemsLayout ItemsLayout
        {
            get => (IItemsLayout)GetValue(ItemsLayoutProperty);
            set => SetValue(ItemsLayoutProperty, value);
        }

        public ItemSizingStrategy ItemSizingStrategy
        {
            get => (ItemSizingStrategy)GetValue(ItemSizingStrategyProperty);
            set => SetValue(ItemSizingStrategyProperty, value);
        }

        public SelectionMode SelectionMode
        {
            get => (SelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public string CollectionStyleResourceKey
        {
            get => (string)GetValue(CollectionStyleResourceKeyProperty);
            set => SetValue(CollectionStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MaterialCollectionView view = (MaterialCollectionView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string collectionStyleResourceKey = string.IsNullOrWhiteSpace(CollectionStyleResourceKey)
                ? DefaultCollectionStyleResourceKey
                : CollectionStyleResourceKey;

            _collection.SetDynamicResource(StyleProperty, collectionStyleResourceKey);
            _collection.ItemsSource = ItemsSource;
            _collection.ItemTemplate = ItemTemplate;
            _collection.ItemsLayout = ItemsLayout;
            _collection.ItemSizingStrategy = ItemSizingStrategy;
            _collection.SelectionMode = SelectionMode;
        }
    }
}
