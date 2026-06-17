using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class ImageViewerViewModel : ViewModelBase
    {
        private readonly CottonFileDownloadResult _file;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly ILogger<ImageViewerViewModel> _logger;
        private string? _status;

        public ImageViewerViewModel(
            string title,
            string details,
            CottonFileDownloadResult file,
            IFileInteractionService fileInteractionService,
            ILogger<ImageViewerViewModel> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(details);
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(logger);

            Title = title;
            Details = details;
            _file = file;
            _fileInteractionService = fileInteractionService;
            _logger = logger;
            ImageSource = ImageSource.FromFile(file.FilePath);
            ShareCommand = new AsyncCommand(ShareAsync);
            OpenExternallyCommand = new AsyncCommand(OpenExternallyAsync);
        }

        public string Title { get; }

        public string Details { get; }

        public ImageSource ImageSource { get; }

        public AsyncCommand ShareCommand { get; }

        public AsyncCommand OpenExternallyCommand { get; }

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
            Status = "Preparing share...";

            try
            {
                await _fileInteractionService.ShareAsync(_file);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to share image viewer file {FilePath}.", _file.FilePath);
                Status = "Share failed.";
            }
        }

        private async Task OpenExternallyAsync()
        {
            Status = "Opening...";

            try
            {
                await _fileInteractionService.OpenAsync(_file);
                Status = null;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open image viewer file {FilePath}.", _file.FilePath);
                Status = "Open failed.";
            }
        }
    }
}
