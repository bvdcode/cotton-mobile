// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;

namespace Cotton.Mobile.Controls
{
    public class IconView : MaterialThemeContentView
    {
        private const string IconColorAnimationName = "M3IconViewColor";

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(IconView),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor),
            typeof(Color),
            typeof(IconView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOnSurface", "M3DarkOnSurface"));

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(IconView),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3FileInlineIconSize"));

        private readonly MauiPath _path;
        private bool _hasAppliedVisualState;

        public IconView()
        {
            _path = new MauiPath
            {
                Aspect = Stretch.Uniform,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };

            Content = _path;
            InputTransparent = true;
            UpdateVisualState();
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public Color IconColor
        {
            get => MaterialResources.ResolveThemeColor(
                this,
                IconColorProperty,
                "M3LightOnSurface",
                "M3DarkOnSurface");
            set => SetValue(IconColorProperty, value);
        }

        protected override void OnRequestedThemeChanged(AppThemeChangedEventArgs e)
        {
            UpdateVisualState();
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            IconView iconView = (IconView)bindable;
            iconView.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_path is null)
            {
                return;
            }

            WidthRequest = IconSize;
            HeightRequest = IconSize;
            _path.WidthRequest = IconSize;
            _path.HeightRequest = IconSize;
            _path.Data = IconData;
            MaterialMotion.UpdateColor(
                _path,
                ResolveCurrentIconColor(),
                IconColor,
                MaterialResources.Get<int>("M3MotionStatusDuration"),
                IconColorAnimationName,
                _hasAppliedVisualState,
                color => _path.Fill = new SolidColorBrush(color));
            _hasAppliedVisualState = true;
        }

        private Color ResolveCurrentIconColor()
        {
            if (_path.Fill is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color;
            }

            return IconColor;
        }
    }
}
