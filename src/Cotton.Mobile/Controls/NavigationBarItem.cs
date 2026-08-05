// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public partial class NavigationBarItem : PressableContentView
    {
        private const string BackgroundAnimationName = "M3NavigationBarItemBackground";
        private const string BorderColorAnimationName = "M3NavigationBarItemBorderColor";
        private const string LabelTextColorAnimationName = "M3NavigationBarItemTextColor";
        private const string OpacityAnimationName = "M3NavigationBarItemOpacity";
        private const string SelectedItemStyleResourceKey = "M3NavigationBarItemSelected";
        private const string UnselectedItemStyleResourceKey = "M3NavigationBarItemUnselected";

        private readonly Border _container;
        private readonly VerticalStackLayout _content;
        private readonly IconView _icon;
        private readonly Label _label;
        private bool _isApplyingSelection;
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
            item.UpdateVisualState(!item._isApplyingSelection);
        }

        private static void OnSelectionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            item.ApplySelectionStyle();
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

        protected override void OnRequestedThemeChanged(AppThemeChangedEventArgs e)
        {
            UpdateVisualState(false);
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

        private void ApplySelectionStyle()
        {
            _isApplyingSelection = true;
            try
            {
                SetDynamicResource(
                    StyleProperty,
                    IsSelected ? SelectedItemStyleResourceKey : UnselectedItemStyleResourceKey);
            }
            finally
            {
                _isApplyingSelection = false;
            }

            UpdateVisualState(false);
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
