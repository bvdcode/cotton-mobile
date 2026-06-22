// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Windows.Input;

namespace Cotton.Mobile.Commands
{
    public class AsyncCommand<T> : ICommand
        where T : class
    {
        private readonly Func<T, Task> _executeAsync;
        private readonly Action<Exception> _onUnhandledException;
        private readonly Func<T, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncCommand(
            Func<T, Task> executeAsync,
            Action<Exception> onUnhandledException,
            Func<T, bool>? canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(executeAsync);
            ArgumentNullException.ThrowIfNull(onUnhandledException);

            _executeAsync = executeAsync;
            _onUnhandledException = onUnhandledException;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            try
            {
                return parameter is T typedParameter
                    && !_isExecuting
                    && (_canExecute?.Invoke(typedParameter) ?? true);
            }
            catch (Exception exception)
            {
                _onUnhandledException(exception);
                return false;
            }
        }

        public async void Execute(object? parameter)
        {
            if (parameter is not T typedParameter || !CanExecute(typedParameter))
            {
                return;
            }

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _executeAsync(typedParameter);
            }
            catch (Exception exception)
            {
                _onUnhandledException(exception);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
