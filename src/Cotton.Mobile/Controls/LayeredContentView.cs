// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class LayeredContentView : ContentView
    {
        private const string DefaultGridStyleResourceKey = "M3LayeredContent";
        private const string LayerOpacityAnimationName = "M3LayeredContentOpacity";

        public static readonly BindableProperty GridStyleResourceKeyProperty = BindableProperty.Create(
            nameof(GridStyleResourceKey),
            typeof(string),
            typeof(LayeredContentView),
            DefaultGridStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IsLayerVisibleProperty = BindableProperty.Create(
            nameof(IsLayerVisible),
            typeof(bool),
            typeof(LayeredContentView),
            true,
            propertyChanged: OnLayerVisiblePropertyChanged);

        private readonly Grid _grid;
        private bool _hasAppliedLayerVisibility;

        public LayeredContentView()
        {
            _grid = new Grid();

            Content = _grid;
            UpdateVisualState();
            UpdateLayerVisibility(false);
        }

        public IList<IView> Items => _grid.Children;

        public bool IsLayerVisible
        {
            get => (bool)GetValue(IsLayerVisibleProperty);
            set => SetValue(IsLayerVisibleProperty, value);
        }

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

        private static void OnLayerVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            LayeredContentView view = (LayeredContentView)bindable;
            view.UpdateLayerVisibility(true);
        }

        private void UpdateVisualState()
        {
            string gridStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                GridStyleResourceKey,
                DefaultGridStyleResourceKey);

            _grid.SetDynamicResource(StyleProperty, gridStyleResourceKey);
        }

        private void UpdateLayerVisibility(bool animateLayerVisibility)
        {
            bool isLayerVisible = IsLayerVisible;
            bool shouldAnimate = animateLayerVisibility && _hasAppliedLayerVisibility;
            double targetOpacity = isLayerVisible
                ? MaterialMotion.Value("M3MotionVisibleOpacity")
                : MaterialMotion.Value("M3MotionHiddenOpacity");
            int duration = MaterialResources.Get<int>("M3MotionStatusDuration");

            InputTransparent = !isLayerVisible;

            if (isLayerVisible)
            {
                IsVisible = true;
            }

            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                LayerOpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity,
                CompleteLayerVisibility);
            _hasAppliedLayerVisibility = true;
        }

        private void CompleteLayerVisibility()
        {
            IsVisible = IsLayerVisible;
        }
    }
}
