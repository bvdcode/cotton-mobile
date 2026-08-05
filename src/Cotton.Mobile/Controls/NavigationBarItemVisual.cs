// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class NavigationBarItemVisual : Border
    {
        private const string BackgroundAnimationName = "M3NavigationBarItemBackground";
        private const string BorderColorAnimationName = "M3NavigationBarItemBorderColor";
        private const string LabelTextColorAnimationName = "M3NavigationBarItemTextColor";
        private const string SelectedStyleResourceKey = "M3NavigationBarItemVisualSelected";
        private const string UnselectedStyleResourceKey = "M3NavigationBarItemVisualUnselected";

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor),
            typeof(Color),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor(
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor(
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty PressedFillColorProperty = BindableProperty.Create(
            nameof(PressedFillColor),
            typeof(Color),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor(
                "M3LightSurfaceContainerHigh",
                "M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty ContentSpacingProperty = BindableProperty.Create(
            nameof(ContentSpacing),
            typeof(double),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarContentSpacing"));

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarIconSize"));

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(NavigationBarItemVisual),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarLabelFontSize"));

        public static readonly BindableProperty TextFontAttributesProperty = BindableProperty.Create(
            nameof(TextFontAttributes),
            typeof(FontAttributes),
            typeof(NavigationBarItemVisual),
            FontAttributes.None,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextFontFamilyProperty = BindableProperty.Create(
            nameof(TextFontFamily),
            typeof(string),
            typeof(NavigationBarItemVisual),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        private readonly VerticalStackLayout _content;
        private readonly IconView _icon;
        private readonly Label _label;
        private bool _hasAppliedVisualState;
        private bool _isApplyingSelection;
        private bool _isPressed;
        private int _duration;

        public NavigationBarItemVisual()
        {
            _icon = new IconView
            {
                HorizontalOptions = LayoutOptions.Center,
            };
            _label = new Label
            {
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1,
                InputTransparent = true,
            };
            _content = new VerticalStackLayout
            {
                InputTransparent = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    _icon,
                    _label,
                },
            };

            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Center;
            Content = _content;
        }

        public Color IconColor
        {
            get => MaterialResources.ResolveThemeColor(
                this,
                IconColorProperty,
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant");
            set => SetValue(IconColorProperty, value);
        }

        public string TextFontFamily
        {
            get => (string)GetValue(TextFontFamilyProperty);
            set => SetValue(TextFontFamilyProperty, value);
        }

        public FontAttributes TextFontAttributes
        {
            get => (FontAttributes)GetValue(TextFontAttributesProperty);
            set => SetValue(TextFontAttributesProperty, value);
        }

        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        public Color TextColor
        {
            get => MaterialResources.ResolveThemeColor(
                this,
                TextColorProperty,
                "M3LightOnSurfaceVariant",
                "M3DarkOnSurfaceVariant");
            set => SetValue(TextColorProperty, value);
        }

        public Color FillColor
        {
            get => (Color)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public Color PressedFillColor
        {
            get => MaterialResources.ResolveThemeColor(
                this,
                PressedFillColorProperty,
                "M3LightSurfaceContainerHigh",
                "M3DarkSurfaceContainerHigh");
            set => SetValue(PressedFillColorProperty, value);
        }

        public Color BorderColor
        {
            get => (Color)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }

        public double ContentSpacing
        {
            get => (double)GetValue(ContentSpacingProperty);
            set => SetValue(ContentSpacingProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public void ApplySelection(bool isSelected)
        {
            _isApplyingSelection = true;
            try
            {
                SetDynamicResource(
                    StyleProperty,
                    isSelected ? SelectedStyleResourceKey : UnselectedStyleResourceKey);
            }
            finally
            {
                _isApplyingSelection = false;
            }

            UpdateVisualState(_isPressed, _duration, animate: false);
        }

        public void SetContent(Geometry? iconData, string text)
        {
            _icon.IconData = iconData;
            _label.Text = text;
        }

        public void UpdateVisualState(bool isPressed, int duration, bool animate)
        {
            _isPressed = isPressed;
            _duration = duration;
            bool shouldAnimate = animate && _hasAppliedVisualState;
            MaterialMotion.UpdateBackgroundColor(
                this,
                isPressed ? PressedFillColor : FillColor,
                duration,
                BackgroundAnimationName,
                shouldAnimate);
            MaterialMotion.UpdateColor(
                this,
                ResolveCurrentBorderColor(),
                BorderColor,
                duration,
                BorderColorAnimationName,
                shouldAnimate,
                color => Stroke = new SolidColorBrush(color));
            _content.Spacing = ContentSpacing;
            _icon.IconColor = IconColor;
            _icon.IconSize = IconSize;
            MaterialMotion.UpdateTextColor(
                _label,
                TextColor,
                MaterialResources.Get<int>("M3MotionStatusDuration"),
                LabelTextColorAnimationName,
                shouldAnimate);
            _label.FontSize = TextFontSize;
            _label.FontAttributes = TextFontAttributes;
            _label.FontFamily = TextFontFamily;
            _hasAppliedVisualState = true;
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItemVisual visual = (NavigationBarItemVisual)bindable;
            visual.UpdateVisualState(
                visual._isPressed,
                visual._duration,
                animate: !visual._isApplyingSelection);
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
