// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class IconFrame : ContentView
    {
        private const string BorderColorAnimationName = "M3IconFrameBorderColor";
        private const string FrameBackgroundAnimationName = "M3IconFrameBackground";

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(IconFrame),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor),
            typeof(Color),
            typeof(IconFrame),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOnSurfaceVariant", "M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty FrameBackgroundColorProperty = BindableProperty.Create(
            nameof(FrameBackgroundColor),
            typeof(Color),
            typeof(IconFrame),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceVariant", "M3DarkSurfaceVariant"));

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(IconFrame),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOutlineVariant", "M3DarkOutlineVariant"));

        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
            nameof(BorderWidth),
            typeof(double),
            typeof(IconFrame),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeThin"));

        public static readonly BindableProperty FrameSizeProperty = BindableProperty.Create(
            nameof(FrameSize),
            typeof(double),
            typeof(IconFrame),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3FileThumbnailSize"));

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(IconFrame),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3FolderThumbnailIconSize"));

        public static readonly BindableProperty FrameCornerRadiusProperty = BindableProperty.Create(
            nameof(FrameCornerRadius),
            typeof(double),
            typeof(IconFrame),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3ThumbnailFrameCornerRadius"));

        private readonly Border _container;
        private readonly IconView _icon;
        private bool _hasAppliedVisualState;

        public IconFrame()
        {
            _icon = new IconView();
            _container = new Border
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = _icon,
            };

            Content = _container;
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
            get => (Color)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }

        public Color FrameBackgroundColor
        {
            get => (Color)GetValue(FrameBackgroundColorProperty);
            set => SetValue(FrameBackgroundColorProperty, value);
        }

        public Color BorderColor
        {
            get => (Color)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }

        public double BorderWidth
        {
            get => (double)GetValue(BorderWidthProperty);
            set => SetValue(BorderWidthProperty, value);
        }

        public double FrameSize
        {
            get => (double)GetValue(FrameSizeProperty);
            set => SetValue(FrameSizeProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double FrameCornerRadius
        {
            get => (double)GetValue(FrameCornerRadiusProperty);
            set => SetValue(FrameCornerRadiusProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            IconFrame iconFrame = (IconFrame)bindable;
            iconFrame.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_container is null || _icon is null)
            {
                return;
            }

            WidthRequest = FrameSize;
            HeightRequest = FrameSize;
            MinimumWidthRequest = FrameSize;
            MinimumHeightRequest = FrameSize;

            _container.WidthRequest = FrameSize;
            _container.HeightRequest = FrameSize;
            _container.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(FrameCornerRadius),
            };
            MaterialMotion.UpdateBackgroundColor(
                _container,
                FrameBackgroundColor,
                MaterialResources.Get<int>("M3MotionStatusDuration"),
                FrameBackgroundAnimationName,
                _hasAppliedVisualState);
            MaterialMotion.UpdateColor(
                _container,
                ResolveCurrentBorderColor(),
                BorderColor,
                MaterialResources.Get<int>("M3MotionStatusDuration"),
                BorderColorAnimationName,
                _hasAppliedVisualState,
                color => _container.Stroke = new SolidColorBrush(color));
            _container.StrokeThickness = BorderWidth;

            _icon.IconData = IconData;
            _icon.IconColor = IconColor;
            _icon.IconSize = IconSize;
            _hasAppliedVisualState = true;
        }

        private Color ResolveCurrentBorderColor()
        {
            if (_container.Stroke is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color;
            }

            return BorderColor;
        }
    }
}
