using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class MediaViewerViewModel : ViewModelBase
    {
        private readonly CottonFileDownloadResult _file;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly ILogger<MediaViewerViewModel> _logger;
        private bool _isBusy;
        private string? _status;

        public MediaViewerViewModel(
            string title,
            string details,
            CottonFilePreviewKind previewKind,
            CottonFileDownloadResult file,
            IFileInteractionService fileInteractionService,
            ILogger<MediaViewerViewModel> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(details);
            if (previewKind is not CottonFilePreviewKind.Audio and not CottonFilePreviewKind.Video)
            {
                throw new ArgumentException("Media viewer requires audio or video preview kind.", nameof(previewKind));
            }

            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(logger);

            Title = title;
            Details = details;
            PreviewKind = previewKind;
            _file = file;
            _fileInteractionService = fileInteractionService;
            _logger = logger;
            MediaSourcePath = $"filesystem://{file.FilePath}";
            MediaLabel = previewKind == CottonFilePreviewKind.Audio ? "Audio" : "Video";
            ShareCommand = new AsyncCommand(ShareAsync, LogUnhandledCommandException, () => !IsBusy);
            OpenExternallyCommand = new AsyncCommand(OpenExternallyAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public string Title { get; }

        public string Details { get; }

        public CottonFilePreviewKind PreviewKind { get; }

        public string MediaSourcePath { get; }

        public string MediaLabel { get; }

        public AsyncCommand ShareCommand { get; }

        public AsyncCommand OpenExternallyCommand { get; }

        public bool IsAudioPreview => PreviewKind == CottonFilePreviewKind.Audio;

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

        private async Task ShareAsync()
        {
            await RunViewerActionAsync(
                "Preparing share...",
                () => _fileInteractionService.ShareAsync(_file),
                "Share failed.",
                "Failed to share media viewer file {FilePath}.");
        }

        private async Task OpenExternallyAsync()
        {
            await RunViewerActionAsync(
                "Opening...",
                () => _fileInteractionService.OpenAsync(_file),
                "Open failed.",
                "Failed to open media viewer file {FilePath}.");
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
                Status = null;
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
            ShareCommand.RaiseCanExecuteChanged();
            OpenExternallyCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile media viewer command exception.");
        }
    }
}
