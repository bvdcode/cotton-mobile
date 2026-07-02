// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class SelectionOverlayView : ContentView
    {
        private const string DefaultOverlayStyleResourceKey = "M3FileSelectionOverlay";
        private const string SelectionOverlayOpacityAnimationName = "M3FileSelectionOverlayOpacity";

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(SelectionOverlayView),
            false,
            propertyChanged: OnSelectedPropertyChanged);

        public static readonly BindableProperty OverlayStyleResourceKeyProperty = BindableProperty.Create(
            nameof(OverlayStyleResourceKey),
            typeof(string),
            typeof(SelectionOverlayView),
            DefaultOverlayStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _overlay;
        private bool _hasAppliedSelectionState;

        public SelectionOverlayView()
        {
            _overlay = new Border();

            InputTransparent = true;
            Content = _overlay;
            UpdateVisualState(animateSelection: false);
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
            view.UpdateVisualState(animateSelection: false);
        }

        private static void OnSelectedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            SelectionOverlayView view = (SelectionOverlayView)bindable;
            view.UpdateVisualState(animateSelection: true);
        }

        private void UpdateVisualState(bool animateSelection)
        {
            string overlayStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                OverlayStyleResourceKey,
                DefaultOverlayStyleResourceKey);

            _overlay.SetDynamicResource(StyleProperty, overlayStyleResourceKey);
            _overlay.IsVisible = true;
            IsVisible = true;
            UpdateSelectionState(animateSelection);
        }

        private void UpdateSelectionState(bool animateSelection)
        {
            double targetOpacity = IsSelected
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            bool shouldAnimate = animateSelection && _hasAppliedSelectionState;

            MaterialMotion.UpdateDouble(
                _overlay,
                _overlay.Opacity,
                targetOpacity,
                MaterialResources.Get<int>("M3MotionSelectionDuration"),
                SelectionOverlayOpacityAnimationName,
                shouldAnimate,
                opacity => _overlay.Opacity = opacity);
            _hasAppliedSelectionState = true;
        }
    }
}
