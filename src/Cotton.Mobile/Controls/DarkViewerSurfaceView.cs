// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    [ContentProperty(nameof(Items))]
    public class DarkViewerSurfaceView : ContentView
    {
        private const string DefaultSurfaceStyleResourceKey = "M3DarkViewerSurface";

        public static readonly BindableProperty SurfaceStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SurfaceStyleResourceKey),
            typeof(string),
            typeof(DarkViewerSurfaceView),
            DefaultSurfaceStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly Grid _surface;

        public DarkViewerSurfaceView()
        {
            _surface = new Grid();

            Content = _surface;
            UpdateVisualState();
        }

        public IList<IView> Items => _surface.Children;

        public string SurfaceStyleResourceKey
        {
            get => (string)GetValue(SurfaceStyleResourceKeyProperty);
            set => SetValue(SurfaceStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            DarkViewerSurfaceView view = (DarkViewerSurfaceView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string surfaceStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                SurfaceStyleResourceKey,
                DefaultSurfaceStyleResourceKey);

            _surface.SetDynamicResource(StyleProperty, surfaceStyleResourceKey);
        }
    }
}
