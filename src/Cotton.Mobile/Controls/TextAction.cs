// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    public class TextAction : CommandPressableContentView
    {
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
            Colors.White,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(TextAction),
            12.0,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
            nameof(ContentPadding),
            typeof(Thickness),
            typeof(TextAction),
            default(Thickness),
            propertyChanged: OnVisualPropertyChanged);

        private readonly Border _container;
        private readonly Label _label;

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
                BackgroundColor = Colors.Transparent,
                StrokeThickness = 0,
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
            TextAction textAction = (TextAction)bindable;
            textAction.UpdateVisualState();
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

            Opacity = ResolvePressableOpacity(1);

            _container.Padding = ContentPadding;
            _container.MinimumHeightRequest = MinimumHeightRequest;
            _container.MinimumWidthRequest = MinimumWidthRequest;

            _label.Text = Text;
            _label.TextColor = TextColor;
            _label.FontSize = TextFontSize;
        }
    }
}
