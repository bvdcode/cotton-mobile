// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class NavigationBarItem : PressableContentView
    {
        private const string BackgroundAnimationName = "M3NavigationBarItemBackground";
        private const string BorderColorAnimationName = "M3NavigationBarItemBorderColor";
        private const string OpacityAnimationName = "M3NavigationBarItemOpacity";

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
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkOnSurfaceVariant"));

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
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkOnSurfaceVariant"));

        public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
            nameof(FillColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty PressedFillColorProperty = BindableProperty.Create(
            nameof(PressedFillColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3DarkSurfaceContainerHigh"));

        public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
            nameof(BorderColor),
            typeof(Color),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Color>("M3Transparent"));

        public static readonly BindableProperty BorderWidthProperty = BindableProperty.Create(
            nameof(BorderWidth),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3StrokeThin"));

        public static readonly BindableProperty ContentSpacingProperty = BindableProperty.Create(
            nameof(ContentSpacing),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarContentSpacing"));

        public static readonly BindableProperty ContentPaddingProperty = BindableProperty.Create(
            nameof(ContentPadding),
            typeof(Thickness),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<Thickness>("M3NavigationBarItemPadding"));

        public static readonly BindableProperty IconSizeProperty = BindableProperty.Create(
            nameof(IconSize),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarIconSize"));

        public static readonly BindableProperty ItemCornerRadiusProperty = BindableProperty.Create(
            nameof(ItemCornerRadius),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("ShapeExtraLarge"));

        public static readonly BindableProperty ItemHeightProperty = BindableProperty.Create(
            nameof(ItemHeight),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarHeight"));

        public static readonly BindableProperty TextFontSizeProperty = BindableProperty.Create(
            nameof(TextFontSize),
            typeof(double),
            typeof(NavigationBarItem),
            propertyChanged: OnVisualPropertyChanged,
            defaultValueCreator: _ => MaterialResources.Get<double>("M3NavigationBarLabelFontSize"));

        public static readonly BindableProperty TextFontAttributesProperty = BindableProperty.Create(
            nameof(TextFontAttributes),
            typeof(FontAttributes),
            typeof(NavigationBarItem),
            FontAttributes.None,
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

        private readonly Border _container;
        private readonly VerticalStackLayout _content;
        private readonly IconView _icon;
        private readonly Label _label;
        private bool _hasAppliedVisualState;
        private ICommand? _observedCommand;

        public NavigationBarItem()
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

            _container = new Border
            {
                StrokeThickness = BorderWidth,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(ItemCornerRadius),
                },
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Center,
                Content = _content,
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

        public Color PressedFillColor
        {
            get => (Color)GetValue(PressedFillColorProperty);
            set => SetValue(PressedFillColorProperty, value);
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

        public double ContentSpacing
        {
            get => (double)GetValue(ContentSpacingProperty);
            set => SetValue(ContentSpacingProperty, value);
        }

        public Thickness ContentPadding
        {
            get => (Thickness)GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public double ItemCornerRadius
        {
            get => (double)GetValue(ItemCornerRadiusProperty);
            set => SetValue(ItemCornerRadiusProperty, value);
        }

        public double ItemHeight
        {
            get => (double)GetValue(ItemHeightProperty);
            set => SetValue(ItemHeightProperty, value);
        }

        public double TextFontSize
        {
            get => (double)GetValue(TextFontSizeProperty);
            set => SetValue(TextFontSizeProperty, value);
        }

        public FontAttributes TextFontAttributes
        {
            get => (FontAttributes)GetValue(TextFontAttributesProperty);
            set => SetValue(TextFontAttributesProperty, value);
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
                UpdateVisualState(true);
            }
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            item.UpdateVisualState(false);
        }

        private static void OnCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            ICommand? oldCommand = oldValue as ICommand;
            ICommand? newCommand = newValue as ICommand;

            item.ObserveCommand(oldCommand, newCommand);
            item.UpdateVisualState(true);
        }

        protected override bool CanHandlePress()
        {
            return IsEnabled && CanExecuteCommand();
        }

        protected override void OnPressedStateChanged()
        {
            UpdateVisualState(true);
        }

        protected override void ExecutePress()
        {
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
            UpdateVisualState(true);
        }

        private void UpdateVisualState(bool animateState)
        {
            if (_container is null || _content is null || _icon is null || _label is null)
            {
                return;
            }

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
            MaterialMotion.UpdateBackgroundColor(
                _container,
                IsPressed ? PressedFillColor : FillColor,
                duration,
                BackgroundAnimationName,
                shouldAnimate);
            _container.HeightRequest = ItemHeight;
            _container.Padding = ContentPadding;
            MaterialMotion.UpdateColor(
                _container,
                ResolveCurrentBorderColor(),
                BorderColor,
                duration,
                BorderColorAnimationName,
                shouldAnimate,
                color => _container.Stroke = new SolidColorBrush(color));
            _container.StrokeThickness = BorderWidth;
            _container.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(ItemCornerRadius),
            };
            _content.Spacing = ContentSpacing;
            _icon.IconData = IconData;
            _icon.IconColor = IconColor;
            _icon.IconSize = IconSize;
            _label.Text = Text;
            _label.TextColor = TextColor;
            _label.FontSize = TextFontSize;
            _label.FontAttributes = TextFontAttributes;
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
