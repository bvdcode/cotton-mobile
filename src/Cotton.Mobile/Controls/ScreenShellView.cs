// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class ScreenShellView : ContentView
    {
        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(ScreenShellView),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;

        public ScreenShellView()
        {
            _grid = new Grid();
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

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
            ScreenShellView view = (ScreenShellView)bindable;
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
