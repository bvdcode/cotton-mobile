// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class InitialsButton : CommandPressableContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(InitialsButton),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Accent"));

        public static readonly BindableProperty ButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(ButtonBackgroundColor),
            typeof(Color),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkSurfaceContainerLow"));

        public static readonly BindableProperty PressedButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(PressedButtonBackgroundColor),
            typeof(Color),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkOutlineVariant"));

        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
            nameof(BorderWidth),
            typeof(double),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeThin"));

        public static readonly BindableProperty ButtonSizeProperty = BindableProperty.Create(
            nameof(ButtonSize),
            typeof(double),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3FileActionSize"));

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3ButtonFontSize"));

        public static readonly BindableProperty ButtonCornerRadiusProperty = BindableProperty.Create(
            nameof(ButtonCornerRadius),
            typeof(double),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("ShapeExtraLarge"));

        public static readonly BindableProperty ButtonOpacityProperty = BindableProperty.Create(
            nameof(ButtonOpacity),
            typeof(double),
            typeof(InitialsButton),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3InteractionEnabledOpacity"));

        private readonly Border _container;
        private readonly Label _label;

        public InitialsButton()
        {
            _label = new Label
            {
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1,
                VerticalOptions = LayoutOptions.Center,
                VerticalTextAlignment = TextAlignment.Center,
            };

            _container = new Border
            {
                StrokeThickness = BorderWidth,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = _label,
            };

            Content = _container;
            UpdateVisualState();
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

        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
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
                UpdateVisualState();
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            InitialsButton initialsButton = (InitialsButton)bindable;
            initialsButton.UpdateVisualState();
        }

        protected override void OnPressedStateChanged()
        {
            UpdateVisualState();
        }

        protected override void OnCommandStateChanged()
        {
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_container is null || _label is null)
            {
                return;
            }

            WidthRequest = ButtonSize;
            HeightRequest = ButtonSize;
            MinimumWidthRequest = ButtonSize;
            MinimumHeightRequest = ButtonSize;
            Opacity = ResolvePressableOpacity(ButtonOpacity);

            _container.WidthRequest = ButtonSize;
            _container.HeightRequest = ButtonSize;
            _container.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(ButtonCornerRadius),
            };
            _container.BackgroundColor = IsPressed ? PressedButtonBackgroundColor : ButtonBackgroundColor;
            _container.Stroke = new SolidColorBrush(BorderColor);
            _container.StrokeThickness = BorderWidth;

            _label.WidthRequest = ButtonSize;
            _label.HeightRequest = ButtonSize;
            _label.Text = Text;
            _label.TextColor = TextColor;
            _label.FontSize = TextFontSize;
        }
    }
}
