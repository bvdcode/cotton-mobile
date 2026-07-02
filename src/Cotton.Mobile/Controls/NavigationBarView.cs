// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class NavigationBarView : ContentView
    {
        private const int DefaultColumnCount = 4;
        private const string DefaultGridStyleResourceKey = "M3NavigationBarGrid";
        private const string DefaultSurfaceStyleResourceKey = "M3NavigationBarSurface";

        public static readonly BindableProperty ColumnCountProperty = BindableProperty.Create(
            nameof(ColumnCount),
            typeof(int),
            typeof(NavigationBarView),
            DefaultColumnCount,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(NavigationBarView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SurfaceStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SurfaceStyleResourceKey),
            typeof(string),
            typeof(NavigationBarView),
            DefaultSurfaceStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;
        private readonly Border _surface;

        public NavigationBarView()
        {
            _grid = new Grid();
            _surface = new Border
            {
                Content = _grid,
            };

            Content = _surface;
            UpdateVisualState();
        }

        public IList<IView> Items => _grid.Children;

        public int ColumnCount
        {
            get => (int)GetValue(ColumnCountProperty);
            set => SetValue(ColumnCountProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        public string SurfaceStyleResourceKey
        {
            get => (string)GetValue(SurfaceStyleResourceKeyProperty);
            set => SetValue(SurfaceStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarView view = (NavigationBarView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? DefaultGridStyleResourceKey
                : GridStyleResourceKey;
            string surfaceStyleResourceKey = string.IsNullOrWhiteSpace(SurfaceStyleResourceKey)
                ? DefaultSurfaceStyleResourceKey
                : SurfaceStyleResourceKey;
            int columnCount = Math.Max(1, ColumnCount);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            _surface.SetDynamicResource(StyleProperty, surfaceStyleResourceKey);
            UpdateColumnDefinitions(columnCount);
        }

        private void UpdateColumnDefinitions(int columnCount)
        {
            if (_grid.ColumnDefinitions.Count == columnCount)
            {
                return;
            }

            _grid.ColumnDefinitions.Clear();

            for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Star,
                });
            }
        }
    }
}
