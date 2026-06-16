using System.Windows.Input;

namespace Cotton.Mobile.Commands
{
    public class AsyncCommand<T> : ICommand
        where T : class
    {
        private readonly Func<T, Task> _executeAsync;
        private readonly Func<T, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncCommand(Func<T, Task> executeAsync, Func<T, bool>? canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(executeAsync);

            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return parameter is T typedParameter
                && !_isExecuting
                && (_canExecute?.Invoke(typedParameter) ?? true);
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
