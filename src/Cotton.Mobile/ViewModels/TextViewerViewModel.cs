using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Cotton.Mobile.ViewModels
{
    public class TextViewerViewModel : ViewModelBase
    {
        private readonly CottonFileDownloadResult _file;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly IClipboard _clipboard;
        private readonly ILogger<TextViewerViewModel> _logger;
        private bool _isBusy;
        private string? _status;

        public TextViewerViewModel(
            string title,
            string details,
            string content,
            CottonFileDownloadResult file,
            IFileInteractionService fileInteractionService,
            IClipboard clipboard,
            ILogger<TextViewerViewModel> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(details);
            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(clipboard);
            ArgumentNullException.ThrowIfNull(logger);

            Title = title;
            Details = details;
            Content = content;
            _file = file;
            _fileInteractionService = fileInteractionService;
            _clipboard = clipboard;
            _logger = logger;
            CopyCommand = new AsyncCommand(CopyAsync, LogUnhandledCommandException, () => !IsBusy && Content.Length > 0);
            ShareCommand = new AsyncCommand(ShareAsync, LogUnhandledCommandException, () => !IsBusy);
            OpenExternallyCommand = new AsyncCommand(OpenExternallyAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public string Title { get; }

        public string Details { get; }

        public string Content { get; }

        public AsyncCommand CopyCommand { get; }

        public AsyncCommand ShareCommand { get; }

        public AsyncCommand OpenExternallyCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandStatesChanged();
                }
            }
        }

        public string? Status
        {
            get => _status;
            private set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(IsStatusVisible));
                }
            }
        }

        public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);

        private async Task CopyAsync()
        {
            await RunViewerActionAsync(
                "Copying...",
                async () =>
                {
                    await CopyTextAsync();
                    Status = "Copied.";
                },
                "Copy failed.",
                "Failed to copy text viewer content for {FilePath}.");
        }

        private Task CopyTextAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _clipboard.SetTextAsync(Content);
            });
        }

        private async Task ShareAsync()
        {
            await RunViewerActionAsync(
                "Preparing share...",
                async () =>
                {
                    await _fileInteractionService.ShareAsync(_file);
                    Status = null;
                },
                "Share failed.",
                "Failed to share text viewer file {FilePath}.");
        }

        private async Task OpenExternallyAsync()
        {
            await RunViewerActionAsync(
                "Opening...",
                async () =>
                {
                    await _fileInteractionService.OpenAsync(_file);
                    Status = null;
                },
                "Open failed.",
                "Failed to open text viewer file {FilePath}.");
        }

        private async Task RunViewerActionAsync(
            string busyStatus,
            Func<Task> actionAsync,
            string failureStatus,
            string failureLogMessage)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            Status = busyStatus;
            try
            {
                await actionAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, failureLogMessage, _file.FilePath);
                Status = ViewerFileActionFailureStatus.Create(exception, failureStatus);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RaiseCommandStatesChanged()
        {
            CopyCommand.RaiseCanExecuteChanged();
            ShareCommand.RaiseCanExecuteChanged();
            OpenExternallyCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile text viewer command exception.");
        }
    }
}
