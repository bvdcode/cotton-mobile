// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Controls
{
    public abstract class CommandPressableContentView : PressableContentView
    {
        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(CommandPressableContentView),
            propertyChanged: OnCommandPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(CommandPressableContentView),
            propertyChanged: OnCommandStatePropertyChanged);

        private ICommand? _observedCommand;

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

        protected override bool CanHandlePress()
        {
            return IsEnabled && CanExecuteCommand();
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

        protected virtual void OnCommandStateChanged()
        {
        }

        private static void OnCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CommandPressableContentView commandPressableContentView = (CommandPressableContentView)bindable;
            ICommand? oldCommand = oldValue as ICommand;
            ICommand? newCommand = newValue as ICommand;

            commandPressableContentView.ObserveCommand(oldCommand, newCommand);
            commandPressableContentView.OnCommandStateChanged();
        }

        private static void OnCommandStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            CommandPressableContentView commandPressableContentView = (CommandPressableContentView)bindable;
            commandPressableContentView.OnCommandStateChanged();
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
            OnCommandStateChanged();
        }
    }
}
