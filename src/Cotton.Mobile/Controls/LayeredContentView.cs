// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class LayeredContentView : ContentView
    {
        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(LayeredContentView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;

        public LayeredContentView()
        {
            _grid = new Grid();

            Content = _grid;
            UpdateVisualState();
        }

        public IList<IView> Items => _grid.Children;

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LayeredContentView view = (LayeredContentView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (string.IsNullOrWhiteSpace(GridStyleResourceKey))
            {
                _grid.ClearValue(StyleProperty);
                return;
            }

            _grid.SetDynamicResource(StyleProperty, GridStyleResourceKey);
        }
    }
}
