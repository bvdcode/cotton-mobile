// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class ScreenContentGridView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3ScreenContentGrid";

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(ScreenContentGridView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;

        public ScreenContentGridView()
        {
            _grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star },
                },
            };

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
            ScreenContentGridView view = (ScreenContentGridView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? DefaultGridStyleResourceKey
                : GridStyleResourceKey;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
        }
    }
}
