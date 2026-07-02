// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class ScreenShellView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3ScreenShell";

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(ScreenShellView),
            DefaultGridStyleResourceKey,
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
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                GridStyleResourceKey,
                DefaultGridStyleResourceKey);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
        }
    }
}
