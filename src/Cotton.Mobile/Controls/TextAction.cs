// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class TextAction : CommandPressableContentView
    {
        private const string BackgroundAnimationName = "M3TextActionBackground";
        private const string LabelTextColorAnimationName = "M3TextActionTextColor";
        private const string OpacityAnimationName = "M3TextActionOpacity";

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(TextAction),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(TextAction),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightOnSurfaceVariant", "M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(TextAction),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3FooterLinkFontSize"));

        public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
            nameof(ContentPadding),
            typeof(Thickness),
            typeof(TextAction),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Thickness>("M3FooterLinkPadding"));

        public static readonly BindableProperty ButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(ButtonBackgroundColor),
            typeof(Color),
            typeof(TextAction),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty PressedButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(PressedButtonBackgroundColor),
            typeof(Color),
            typeof(TextAction),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.GetThemeColor("M3LightSurfaceContainerHigh", "M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty ButtonCornerRadiusProperty = BindableProperty.Create(
            nameof(ButtonCornerRadius),
            typeof(double),
            typeof(TextAction),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("ShapeExtraLarge"));

        private readonly Border _container;
        private readonly Label _label;
        private bool _hasAppliedVisualState;

        public TextAction()
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
                StrokeThickness = MaterialResources.Get<double>("M3StrokeNone"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
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

        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
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

        public double ButtonCornerRadius
        {
            get => (double)GetValue(ButtonCornerRadiusProperty);
            set => SetValue(ButtonCornerRadiusProperty, value);
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
            TextAction textAction = (TextAction)bindable;
            textAction.UpdateVisualState(true);
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

            int duration = IsPressed ? PressInDuration : PressOutDuration;
            bool shouldAnimate = animateState && _hasAppliedVisualState;
            MaterialMotion.UpdateDouble(
                this,
                Opacity,
                ResolvePressableOpacity(1),
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
            MaterialMotion.UpdateBackgroundColor(
                _container,
                IsPressed ? PressedButtonBackgroundColor : ButtonBackgroundColor,
                duration,
                BackgroundAnimationName,
                shouldAnimate);

            _label.Text = Text;
            MaterialMotion.UpdateTextColor(
                _label,
                TextColor,
                MaterialResources.Get<int>("M3MotionStatusDuration"),
                LabelTextColorAnimationName,
                shouldAnimate);
            _label.FontSize = TextFontSize;
            _hasAppliedVisualState = true;
        }
    }
}
