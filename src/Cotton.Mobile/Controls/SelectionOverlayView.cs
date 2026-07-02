// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class SelectionOverlayView : ContentView
    {
        private const string DefaultOverlayStyleResourceKey = "M3FileSelectionOverlay";

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(SelectionOverlayView),
            false,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty OverlayStyleResourceKeyProperty = BindableProperty.Create(
            nameof(OverlayStyleResourceKey),
            typeof(string),
            typeof(SelectionOverlayView),
            DefaultOverlayStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _overlay;

        public SelectionOverlayView()
        {
            _overlay = new Border();

            InputTransparent = true;
            Content = _overlay;
            UpdateVisualState();
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public string OverlayStyleResourceKey
        {
            get => (string)GetValue(OverlayStyleResourceKeyProperty);
            set => SetValue(OverlayStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SelectionOverlayView view = (SelectionOverlayView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string overlayStyleResourceKey = string.IsNullOrWhiteSpace(OverlayStyleResourceKey)
                ? DefaultOverlayStyleResourceKey
                : OverlayStyleResourceKey;

            _overlay.SetDynamicResource(StyleProperty, overlayStyleResourceKey);
            _overlay.IsVisible = IsSelected;
            IsVisible = IsSelected;
        }
    }
}
