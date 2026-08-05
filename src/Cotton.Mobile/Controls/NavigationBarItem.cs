// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Cotton.Mobile.Controls
{
    public class NavigationBarItem : PressableContentView
    {
        private const string OpacityAnimationName = "M3NavigationBarItemOpacity";

        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
            nameof(IsSelected),
            typeof(bool),
            typeof(NavigationBarItem),
            false,
            propertyChanged: OnSelectionChanged);

        public static readonly BindableProperty IconDataProperty = BindableProperty.Create(
            nameof(IconData),
            typeof(Geometry),
            typeof(NavigationBarItem),
            default(Geometry),
            propertyChanged: OnContentPropertyChanged);

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(NavigationBarItem),
            string.Empty,
            propertyChanged: OnContentPropertyChanged);

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(NavigationBarItem),
            propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(NavigationBarItem),
            propertyChanged: OnCommandParameterChanged);

        private readonly NavigationBarItemVisual _visual;
        private bool _hasAppliedVisualState;
        private ICommand? _observedCommand;

        public NavigationBarItem()
        {
            _visual = new NavigationBarItemVisual();
            Content = _visual;
            _visual.ApplySelection(IsSelected);
            UpdateContent();
            UpdateVisualState(false);
        }

        public Geometry? IconData
        {
            get => (Geometry?)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
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

        private static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            item.UpdateContent();
        }

        private static void OnSelectionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            item._visual.ApplySelection(item.IsSelected);
            item.UpdateVisualState(false);
        }

        private static void OnCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            item.ObserveCommand(oldValue as ICommand, newValue as ICommand);
            item.UpdateVisualState(true);
        }

        private static void OnCommandParameterChanged(BindableObject bindable, object oldValue, object newValue)
        {
            NavigationBarItem item = (NavigationBarItem)bindable;
            item.UpdateVisualState(true);
        }

        private bool CanExecuteCommand()
        {
            ICommand? command = Command;
            return command is null || command.CanExecute(CommandParameter);
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

        private void UpdateContent()
        {
            if (_visual is null)
            {
                return;
            }

            _visual.SetContent(IconData, Text);
        }

        private void UpdateVisualState(bool animateState)
        {
            if (_visual is null)
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
            _visual.UpdateVisualState(IsPressed, duration, shouldAnimate);
            _hasAppliedVisualState = true;
        }
    }
}
