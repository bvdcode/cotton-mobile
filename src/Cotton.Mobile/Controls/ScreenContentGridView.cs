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

        public static readonly BindableProperty ExtraAutoRowsProperty = BindableProperty.Create(
            nameof(ExtraAutoRows),
            typeof(int),
            typeof(ScreenContentGridView),
            0,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;

        public ScreenContentGridView()
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

        public int ExtraAutoRows
        {
            get => (int)GetValue(ExtraAutoRowsProperty);
            set => SetValue(ExtraAutoRowsProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ScreenContentGridView view = (ScreenContentGridView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                GridStyleResourceKey,
                DefaultGridStyleResourceKey);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            RebuildRows();
        }

        private void RebuildRows()
        {
            int extraAutoRows = Math.Max(0, ExtraAutoRows);
            int autoRows = 2 + extraAutoRows;
            int requiredRows = autoRows + 1;

            if (_grid.RowDefinitions.Count == requiredRows)
            {
                return;
            }

            _grid.RowDefinitions.Clear();

            for (int index = 0; index < autoRows; index++)
            {
                _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
        }
    }
}
