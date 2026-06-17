using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Cotton.Mobile.ViewModels
{
    public class TextViewerViewModel : ViewModelBase
    {
        private readonly CottonFileDownloadResult _file;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly IClipboard _clipboard;
        private readonly ILogger<TextViewerViewModel> _logger;
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
            CopyCommand = new AsyncCommand(CopyAsync, () => Content.Length > 0);
            ShareCommand = new AsyncCommand(ShareAsync);
            OpenExternallyCommand = new AsyncCommand(OpenExternallyAsync);
        }

        public string Title { get; }

        public string Details { get; }

        public string Content { get; }

        public AsyncCommand CopyCommand { get; }

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

        private async Task CopyAsync()
        {
            Status = "Copying...";

            try
            {
                await _clipboard.SetTextAsync(Content);
                Status = "Copied.";
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to copy text viewer content for {FilePath}.", _file.FilePath);
                Status = "Copy failed.";
            }
        }

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
                _logger.LogWarning(exception, "Failed to share text viewer file {FilePath}.", _file.FilePath);
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
                _logger.LogWarning(exception, "Failed to open text viewer file {FilePath}.", _file.FilePath);
                Status = "Open failed.";
            }
        }
    }
}
