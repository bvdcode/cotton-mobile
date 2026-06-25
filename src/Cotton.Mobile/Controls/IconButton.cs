// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class IconButton : ContentView
    {
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
            Colors.White,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ButtonBackgroundColorProperty = BindableProperty.Create(
            nameof(ButtonBackgroundColor),
            typeof(Color),
            typeof(IconButton),
            Colors.Transparent,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(IconButton),
            Colors.Transparent,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ButtonSizeProperty = BindableProperty.Create(
            nameof(ButtonSize),
            typeof(double),
            typeof(IconButton),
            44.0,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(IconButton),
            20.0,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ButtonCornerRadiusProperty = BindableProperty.Create(
            nameof(ButtonCornerRadius),
            typeof(double),
            typeof(IconButton),
            22.0,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty ButtonOpacityProperty = BindableProperty.Create(
            nameof(ButtonOpacity),
            typeof(double),
            typeof(IconButton),
            1.0,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(IconButton));

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(IconButton));

        private const double DisabledOpacity = 0.5;

        private readonly Border _container;
        private readonly IconView _icon;

        public IconButton()
        {
            _icon = new IconView();
            _container = new Border
            {
                StrokeThickness = 1,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Content = _icon,
            };

            TapGestureRecognizer tap = new();
            tap.Tapped += HandleTapped;
            GestureRecognizers.Add(tap);

            Content = _container;
            UpdateVisualState();
        }

        public event EventHandler? Clicked;

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

        public Color BorderColor
        {
            get => (Color)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
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

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
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
            IconButton iconButton = (IconButton)bindable;
            iconButton.UpdateVisualState();
        }

        private void HandleTapped(object? sender, TappedEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            ICommand? command = Command;
            object? parameter = CommandParameter;
            if (command?.CanExecute(parameter) == true)
            {
                command.Execute(parameter);
            }

            Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateVisualState()
        {
            WidthRequest = ButtonSize;
            HeightRequest = ButtonSize;
            MinimumWidthRequest = ButtonSize;
            MinimumHeightRequest = ButtonSize;
            Opacity = IsEnabled ? ButtonOpacity : DisabledOpacity;

            _container.WidthRequest = ButtonSize;
            _container.HeightRequest = ButtonSize;
            _container.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(ButtonCornerRadius),
            };
            _container.BackgroundColor = ButtonBackgroundColor;
            _container.Stroke = new SolidColorBrush(BorderColor);

            _icon.IconData = IconData;
            _icon.IconColor = IconColor;
            _icon.IconSize = IconSize;
        }
    }
}
