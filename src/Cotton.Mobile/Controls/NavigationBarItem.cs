// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class NavigationBarItem : ContentView
    {
        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(NavigationBarItem),
            default(Geometry),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor),
            typeof(Color),
            typeof(NavigationBarItem),
            Colors.White,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(NavigationBarItem),
            string.Empty,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor),
            typeof(Color),
            typeof(NavigationBarItem),
            Colors.White,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(NavigationBarItem),
            Colors.Transparent,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(NavigationBarItem),
            Colors.Transparent,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(NavigationBarItem),
            propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged);

        private const double DisabledOpacity = 0.5;

        private readonly Border _container;
        private readonly IconView _icon;
        private readonly Label _label;
        private ICommand? _observedCommand;

        public NavigationBarItem()
        {
            _icon = new IconView
            {
                IconSize = 18,
                HorizontalOptions = LayoutOptions.Center,
            };

            _label = new Label
            {
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1,
                InputTransparent = true,
            };

            VerticalStackLayout content = new()
            {
                Spacing = 3,
                InputTransparent = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    _icon,
                    _label,
                },
            };

            _container = new Border
            {
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(20),
                },
                Padding = new Thickness(4, 5),
                HeightRequest = 56,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center,
                Content = content,
            };

            TapGestureRecognizer tap = new();
            tap.Tapped += HandleTapped;
            GestureRecognizers.Add(tap);

            Content = _container;
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

        public Color FillColor
        {
            get => (Color)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public Color BorderColor
        {
            get => (Color)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
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
            NavigationBarItem item = (NavigationBarItem)bindable;
            item.UpdateVisualState();
        }

        private static void OnCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            ICommand? oldCommand = oldValue as ICommand;
            ICommand? newCommand = newValue as ICommand;

            item.ObserveCommand(oldCommand, newCommand);
            item.UpdateVisualState();
        }

        private void HandleTapped(object? sender, TappedEventArgs e)
        {
            if (!IsEnabled || !CanExecuteCommand())
            {
                return;
            }

            ICommand? command = Command;
            object? parameter = CommandParameter;
            if (command?.CanExecute(parameter) == true)
            {
                command.Execute(parameter);
            }
        }

        private bool CanExecuteCommand()
        {
            ICommand? command = Command;
            if (command is null)
            {
                return true;
            }

            return command.CanExecute(CommandParameter);
        }

        private void ObserveCommand(ICommand? oldCommand, ICommand? newCommand)
        {
            if (oldCommand is not null && ReferenceEquals(_observedCommand, oldCommand))
            {
                oldCommand.CanExecuteChanged -= OnCommandCanExecuteChanged;
                _observedCommand = null;
            }

            if (newCommand is not null)
            {
                newCommand.CanExecuteChanged += OnCommandCanExecuteChanged;
                _observedCommand = newCommand;
            }
        }

        private void OnCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            Opacity = IsEnabled && CanExecuteCommand() ? 1 : DisabledOpacity;
            _container.BackgroundColor = FillColor;
            _container.Stroke = new SolidColorBrush(BorderColor);
            _icon.IconData = IconData;
            _icon.IconColor = IconColor;
            _label.Text = Text;
            _label.TextColor = TextColor;
        }
    }
}
