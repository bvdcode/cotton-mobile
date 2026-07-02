// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class IconButton : CommandPressableContentView
    {
        private const string BackgroundAnimationName = "M3IconButtonBackground";
        private const string BorderColorAnimationName = "M3IconButtonBorderColor";
        private const string OpacityAnimationName = "M3IconButtonOpacity";

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(IconButton),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor),
            typeof(Color),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOnSurface", "M3DarkOnSurface"));

        public static readonly BindableProperty ButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(ButtonBackgroundColor),
            typeof(Color),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty PressedButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(PressedButtonBackgroundColor),
            typeof(Color),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainerHigh", "M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
            nameof(BorderWidth),
            typeof(double),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeThin"));

        public static readonly BindableProperty ButtonSizeProperty = BindableProperty.Create(
            nameof(ButtonSize),
            typeof(double),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3FileActionSize"));

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3FileActionIconSize"));

        public static readonly BindableProperty ButtonCornerRadiusProperty = BindableProperty.Create(
            nameof(ButtonCornerRadius),
            typeof(double),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("ShapeExtraLarge"));

        public static readonly BindableProperty ButtonOpacityProperty = BindableProperty.Create(
            nameof(ButtonOpacity),
            typeof(double),
            typeof(IconButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3InteractionEnabledOpacity"));

        private readonly Border _container;
        private readonly IconView _icon;
        private bool _hasAppliedVisualState;

        public IconButton()
        {
            _icon = new IconView();
            _container = new Border
            {
                StrokeThickness = BorderWidth,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = _icon,
            };

            Content = _container;
            UpdateVisualState(false);
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

        public Color ButtonBackgroundColor
        {
            get => (Color)GetValue(ButtonBackgroundColorProperty);
            set => SetValue(ButtonBackgroundColorProperty, value);
        }

        public Color PressedButtonBackgroundColor
        {
            get => (Color)GetValue(PressedButtonBackgroundColorProperty);
            set => SetValue(PressedButtonBackgroundColorProperty, value);
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

        public double ButtonSize
        {
            get => (double)GetValue(ButtonSizeProperty);
            set => SetValue(ButtonSizeProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double ButtonCornerRadius
        {
            get => (double)GetValue(ButtonCornerRadiusProperty);
            set => SetValue(ButtonCornerRadiusProperty, value);
        }

        public double ButtonOpacity
        {
            get => (double)GetValue(ButtonOpacityProperty);
            set => SetValue(ButtonOpacityProperty, value);
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.Equals(propertyName, nameof(IsEnabled), StringComparison.Ordinal))
            {
                UpdateVisualState(true);
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            IconButton iconButton = (IconButton)bindable;
            iconButton.UpdateVisualState(false);
        }

        protected override void OnPressedStateChanged()
        {
            UpdateVisualState(true);
        }

        protected override void OnCommandStateChanged()
        {
            UpdateVisualState(true);
        }

        private void UpdateVisualState(bool animateState)
        {
            if (_container is null || _icon is null)
            {
                return;
            }

            double targetOpacity = ResolvePressableOpacity(ButtonOpacity);
            int duration = IsPressed ? PressInDuration : PressOutDuration;
            bool shouldAnimate = animateState && _hasAppliedVisualState;

            WidthRequest = ButtonSize;
            HeightRequest = ButtonSize;
            MinimumWidthRequest = ButtonSize;
            MinimumHeightRequest = ButtonSize;
            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                OpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity);

            _container.WidthRequest = ButtonSize;
            _container.HeightRequest = ButtonSize;
            _container.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(ButtonCornerRadius),
            };
            MaterialMotion.UpdateBackgroundColor(
                _container,
                IsPressed ? PressedButtonBackgroundColor : ButtonBackgroundColor,
                duration,
                BackgroundAnimationName,
                shouldAnimate);
            MaterialMotion.UpdateColor(
                _container,
                ResolveCurrentBorderColor(),
                BorderColor,
                duration,
                BorderColorAnimationName,
                shouldAnimate,
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
