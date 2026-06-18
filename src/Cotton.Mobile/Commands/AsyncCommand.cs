using System.Windows.Input;

namespace Cotton.Mobile.Commands
{
    public class AsyncCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Action<Exception> _onUnhandledException;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncCommand(
            Func<Task> executeAsync,
            Action<Exception> onUnhandledException,
            Func<bool>? canExecute = null)
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
                return !_isExecuting && (_canExecute?.Invoke() ?? true);
            }
            catch (Exception exception)
            {
                _onUnhandledException(exception);
                return false;
            }
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _executeAsync();
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
