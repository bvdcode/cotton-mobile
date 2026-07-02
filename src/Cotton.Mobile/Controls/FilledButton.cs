// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class FilledButton : CommandPressableContentView
    {
        private const string BackgroundAnimationName = "M3FilledButtonBackground";
        private const string BorderColorAnimationName = "M3FilledButtonBorderColor";
        private const string LabelTextColorAnimationName = "M3FilledButtonTextColor";
        private const string OpacityAnimationName = "M3FilledButtonOpacity";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(FilledButton),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3OnAccent"));

        public static readonly BindableProperty DisabledTextColorProperty = BindableProperty.Create(
            nameof(DisabledTextColor),
            typeof(Color),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOnSurfaceVariant", "M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty ButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(ButtonBackgroundColor),
            typeof(Color),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Accent"));

        public static readonly BindableProperty PressedButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(PressedButtonBackgroundColor),
            typeof(Color),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3AccentPressed"));

        public static readonly BindableProperty DisabledButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(DisabledButtonBackgroundColor),
            typeof(Color),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainerHighest", "M3DarkSurfaceContainerHighest"));

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Accent"));

        public static readonly BindableProperty DisabledBorderColorProperty = BindableProperty.Create(
            nameof(DisabledBorderColor),
            typeof(Color),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
            nameof(BorderWidth),
            typeof(double),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeNone"));

        public static readonly BindableProperty ButtonCornerRadiusProperty = BindableProperty.Create(
            nameof(ButtonCornerRadius),
            typeof(double),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => Convert.ToDouble(MaterialResources.Get<int>("M3ButtonCornerRadius"), CultureInfo.InvariantCulture));

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3ButtonFontSize"));

        public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
            nameof(FontAttributes),
            typeof(FontAttributes),
            typeof(FilledButton),
            FontAttributes.Bold,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
            nameof(ContentPadding),
            typeof(Thickness),
            typeof(FilledButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Thickness>("M3FilledButtonPadding"));

        private readonly Border _container;
        private readonly Label _label;
        private bool _hasAppliedVisualState;

        public FilledButton()
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

            _container = new Border
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Content = _label,
            };

            Content = _container;
            UpdateVisualState(false);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public Color DisabledTextColor
        {
            get => (Color)GetValue(DisabledTextColorProperty);
            set => SetValue(DisabledTextColorProperty, value);
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

        public Color DisabledButtonBackgroundColor
        {
            get => (Color)GetValue(DisabledButtonBackgroundColorProperty);
            set => SetValue(DisabledButtonBackgroundColorProperty, value);
        }

        public Color BorderColor
        {
            get => (Color)GetValue(BorderColorProperty);
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

        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
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
            FilledButton filledButton = (FilledButton)bindable;
            filledButton.UpdateVisualState(true);
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
            if (_container is null || _label is null)
            {
                return;
            }

            bool canPress = CanHandlePress();
            double targetOpacity = ResolvePressableOpacity(1);
            int duration = IsPressed ? PressInDuration : PressOutDuration;
            bool shouldAnimate = animateState && _hasAppliedVisualState;
            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                targetOpacity,
                duration,
                OpacityAnimationName,
                shouldAnimate,
                opacity => Opacity = opacity);

            _container.Padding = ContentPadding;
            _container.MinimumHeightRequest = MinimumHeightRequest;
            _container.MinimumWidthRequest = MinimumWidthRequest;
            _container.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(ButtonCornerRadius),
            };
            MaterialMotion.UpdateColor(
                _container,
                ResolveCurrentBorderColor(),
                ResolveBorderColor(canPress),
                duration,
                BorderColorAnimationName,
                shouldAnimate,
                color => _container.Stroke = new SolidColorBrush(color));
            _container.StrokeThickness = BorderWidth;
            MaterialMotion.UpdateBackgroundColor(
                _container,
                ResolveBackgroundColor(canPress),
                duration,
                BackgroundAnimationName,
                shouldAnimate);

            _label.Text = Text;
            MaterialMotion.UpdateTextColor(
                _label,
                canPress ? TextColor : DisabledTextColor,
                MaterialResources.Get<int>("M3MotionStatusDuration"),
                LabelTextColorAnimationName,
                shouldAnimate);
            _label.FontSize = TextFontSize;
            _label.FontAttributes = FontAttributes;
            _hasAppliedVisualState = true;
        }

        private Color ResolveBackgroundColor(bool canPress)
        {
            if (!canPress)
            {
                return DisabledButtonBackgroundColor;
            }

            return IsPressed ? PressedButtonBackgroundColor : ButtonBackgroundColor;
        }

        private Color ResolveBorderColor(bool canPress)
        {
            return canPress ? BorderColor : DisabledBorderColor;
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
