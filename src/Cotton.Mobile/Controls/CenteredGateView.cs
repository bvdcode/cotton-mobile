// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(BodyContent))]
    public class CenteredGateView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3AppLockGateGrid";

        public static readonly BindableProperty BodyContentProperty = BindableProperty.Create(
            nameof(BodyContent),
            typeof(View),
            typeof(CenteredGateView),
            default(View),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(CenteredGateView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _grid;

        public CenteredGateView()
        {
            _grid = new Grid();
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            Content = _grid;
            UpdateVisualState();
        }

        public View? BodyContent
        {
            get => (View?)GetValue(BodyContentProperty);
            set => SetValue(BodyContentProperty, value);
        }

        public string GridStyleResourceKey
        {
            get => (string)GetValue(GridStyleResourceKeyProperty);
            set => SetValue(GridStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CenteredGateView view = (CenteredGateView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = string.IsNullOrWhiteSpace(GridStyleResourceKey)
                ? DefaultGridStyleResourceKey
                : GridStyleResourceKey;

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
            SetBodyContent();
        }

        private void SetBodyContent()
        {
            _grid.Children.Clear();

            if (BodyContent is null)
            {
                return;
            }

            Grid.SetRow(BodyContent, 1);
            _grid.Children.Add(BodyContent);
        }
    }
}
