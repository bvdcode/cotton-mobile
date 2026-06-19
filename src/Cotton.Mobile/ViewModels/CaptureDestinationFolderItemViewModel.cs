using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public class CaptureDestinationFolderItemViewModel
    {
        private readonly CottonFolderHandle _folder;
        private readonly Func<CottonFolderHandle, Task> _openAsync;
        private readonly Action<Exception> _onUnhandledException;

        public CaptureDestinationFolderItemViewModel(
            CottonFolderHandle folder,
            Func<CottonFolderHandle, Task> openAsync,
            Action<Exception> onUnhandledException)
        {
            ArgumentNullException.ThrowIfNull(folder);
            ArgumentNullException.ThrowIfNull(openAsync);
            ArgumentNullException.ThrowIfNull(onUnhandledException);

            _folder = folder;
            _openAsync = openAsync;
            _onUnhandledException = onUnhandledException;
            DisplayName = folder.Name;
            OpenCommand = new AsyncCommand(OpenAsync, _onUnhandledException);
        }

        public string DisplayName { get; }

        public AsyncCommand OpenCommand { get; }

        private Task OpenAsync()
        {
            return _openAsync(_folder);
        }
    }
}
