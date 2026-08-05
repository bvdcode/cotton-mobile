// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class FilledButtonVisual : Border
    {
        private const string BackgroundAnimationName = "M3FilledButtonBackground";
        private const string BorderColorAnimationName = "M3FilledButtonBorderColor";
        private const string LabelTextColorAnimationName = "M3FilledButtonTextColor";
        private const string StyleResourceKey = "M3FilledButtonVisual";

        public static readonly BindableProperty TextColorProperty = CreateThemeColorProperty(
            nameof(TextColor),
            "M3LightOnAction",
            "M3DarkOnAction");
        public static readonly BindableProperty DisabledTextColorProperty = CreateThemeColorProperty(
            nameof(DisabledTextColor),
            "M3LightOnSurfaceVariant",
            "M3DarkOnSurfaceVariant");
        public static readonly BindableProperty ButtonBackgroundColorProperty = CreateThemeColorProperty(
            nameof(ButtonBackgroundColor),
            "M3LightAction",
            "M3DarkAction");
        public static readonly BindableProperty PressedButtonBackgroundColorProperty = CreateThemeColorProperty(
            nameof(PressedButtonBackgroundColor),
            "M3LightActionPressed",
            "M3DarkActionPressed");
        public static readonly BindableProperty DisabledButtonBackgroundColorProperty = CreateThemeColorProperty(
            nameof(DisabledButtonBackgroundColor),
            "M3LightSurfaceContainerHighest",
            "M3DarkSurfaceContainerHighest");
        public static readonly BindableProperty BorderColorProperty = CreateThemeColorProperty(
            nameof(BorderColor),
            "M3LightAction",
            "M3DarkAction");
        public static readonly BindableProperty DisabledBorderColorProperty = BindableProperty.Create(
            nameof(DisabledBorderColor),
            typeof(Color),
            typeof(FilledButtonVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));
        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
            nameof(BorderWidth),
            typeof(double),
            typeof(FilledButtonVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeNone"));
        public static readonly BindableProperty ButtonCornerRadiusProperty = BindableProperty.Create(
            nameof(ButtonCornerRadius),
            typeof(double),
            typeof(FilledButtonVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => Convert.ToDouble(
                MaterialResources.Get<int>("M3ButtonCornerRadius"),
                CultureInfo.InvariantCulture));
        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(FilledButtonVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3ButtonFontSize"));
        public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
            nameof(FontAttributes),
            typeof(FontAttributes),
            typeof(FilledButtonVisual),
            FontAttributes.None,
            propertyChanged: OnVisualPropertyChanged);
        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
            nameof(FontFamily),
            typeof(string),
            typeof(FilledButtonVisual),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);
        public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
            nameof(ContentPadding),
            typeof(Thickness),
            typeof(FilledButtonVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Thickness>("M3FilledButtonPadding"));

        private readonly Label _label;
        private bool _canPress;
        private int _duration;
        private bool _hasAppliedVisualState;
        private bool _isPressed;

        public FilledButtonVisual()
        {
            _label = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1,
                VerticalOptions = LayoutOptions.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };

            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
            Content = _label;
            SetDynamicResource(StyleProperty, StyleResourceKey);
        }

        public Color TextColor
        {
            get => ResolveThemeColor(TextColorProperty, "M3LightOnAction", "M3DarkOnAction");
            set => SetValue(TextColorProperty, value);
        }

        public Color DisabledTextColor
        {
            get => ResolveThemeColor(
                DisabledTextColorProperty,
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant");
            set => SetValue(DisabledTextColorProperty, value);
        }

        public Color ButtonBackgroundColor
        {
            get => ResolveThemeColor(ButtonBackgroundColorProperty, "M3LightAction", "M3DarkAction");
            set => SetValue(ButtonBackgroundColorProperty, value);
        }

        public Color PressedButtonBackgroundColor
        {
            get => ResolveThemeColor(
                PressedButtonBackgroundColorProperty,
                "M3LightActionPressed",
                "M3DarkActionPressed");
            set => SetValue(PressedButtonBackgroundColorProperty, value);
        }

        public Color DisabledButtonBackgroundColor
        {
            get => ResolveThemeColor(
                DisabledButtonBackgroundColorProperty,
                "M3LightSurfaceContainerHighest",
                "M3DarkSurfaceContainerHighest");
            set => SetValue(DisabledButtonBackgroundColorProperty, value);
        }

        public Color BorderColor
        {
            get => ResolveThemeColor(BorderColorProperty, "M3LightAction", "M3DarkAction");
            set => SetValue(BorderColorProperty, value);
        }

        public Color DisabledBorderColor
        {
            get => (Color)GetValue(DisabledBorderColorProperty);
            set => SetValue(DisabledBorderColorProperty, value);
        }

        public double BorderWidth
        {
            get => (double)GetValue(BorderWidthProperty);
            set => SetValue(BorderWidthProperty, value);
        }

        public double ButtonCornerRadius
        {
            get => (double)GetValue(ButtonCornerRadiusProperty);
            set => SetValue(ButtonCornerRadiusProperty, value);
        }

        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        public FontAttributes FontAttributes
        {
            get => (FontAttributes)GetValue(FontAttributesProperty);
            set => SetValue(FontAttributesProperty, value);
        }

        public string FontFamily
        {
            get => (string)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        public void UpdateVisualState(
            string text,
            bool isPressed,
            bool canPress,
            double minimumHeight,
            double minimumWidth,
            int duration,
            bool animate)
        {
            _isPressed = isPressed;
            _canPress = canPress;
            _duration = duration;
            bool shouldAnimate = animate && _hasAppliedVisualState;

            Padding = ContentPadding;
            MinimumHeightRequest = minimumHeight;
            MinimumWidthRequest = minimumWidth;
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(ButtonCornerRadius),
            };
            MaterialMotion.UpdateColor(
                this,
                ResolveCurrentBorderColor(),
                canPress ? BorderColor : DisabledBorderColor,
                duration,
                BorderColorAnimationName,
                shouldAnimate,
                color => Stroke = new SolidColorBrush(color));
            StrokeThickness = BorderWidth;
            MaterialMotion.UpdateBackgroundColor(
                this,
                ResolveBackgroundColor(),
                duration,
                BackgroundAnimationName,
                shouldAnimate);

            _label.Text = text;
            MaterialMotion.UpdateTextColor(
                _label,
                canPress ? TextColor : DisabledTextColor,
                MaterialResources.Get<int>("M3MotionStatusDuration"),
                LabelTextColorAnimationName,
                shouldAnimate);
            _label.FontSize = TextFontSize;
            _label.FontAttributes = FontAttributes;
            _label.FontFamily = FontFamily;
            _hasAppliedVisualState = true;
        }

        private static BindableProperty CreateThemeColorProperty(
            string name,
            string lightResourceKey,
            string darkResourceKey)
        {
            return BindableProperty.Create(
                name,
                typeof(Color),
                typeof(FilledButtonVisual),
                propertyChanged: OnVisualPropertyChanged,
                defaultValueCreator: _ => MaterialResources.GetThemeColor(lightResourceKey, darkResourceKey));
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            FilledButtonVisual visual = (FilledButtonVisual)bindable;
            visual.UpdateVisualState(
                visual._label.Text,
                visual._isPressed,
                visual._canPress,
                visual.MinimumHeightRequest,
                visual.MinimumWidthRequest,
                visual._duration,
                animate: false);
        }

        private Color ResolveThemeColor(
            BindableProperty property,
            string lightResourceKey,
            string darkResourceKey)
        {
            return MaterialResources.ResolveThemeColor(this, property, lightResourceKey, darkResourceKey);
        }

        private Color ResolveBackgroundColor()
        {
            if (!_canPress)
            {
                return DisabledButtonBackgroundColor;
            }

            return _isPressed ? PressedButtonBackgroundColor : ButtonBackgroundColor;
        }

        private Color ResolveCurrentBorderColor()
        {
            if (Stroke is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color;
            }

            return BorderColor;
        }
    }
}
