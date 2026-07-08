// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Commands;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.ViewModels
{
    public class PdfViewerViewModel : ViewModelBase
    {
        private readonly CottonFileDownloadResult _file;
        private readonly IPdfPreviewRenderer _pdfPreviewRenderer;
        private readonly IFileInteractionService _fileInteractionService;
        private readonly ILogger<PdfViewerViewModel> _logger;
        private bool _isBusy;
        private bool _didLoad;
        private string? _status;

        public PdfViewerViewModel(
            string title,
            string details,
            CottonFileDownloadResult file,
            IPdfPreviewRenderer pdfPreviewRenderer,
            IFileInteractionService fileInteractionService,
            ILogger<PdfViewerViewModel> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(details);
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(pdfPreviewRenderer);
            ArgumentNullException.ThrowIfNull(fileInteractionService);
            ArgumentNullException.ThrowIfNull(logger);

            Title = title;
            Details = details;
            _file = file;
            _pdfPreviewRenderer = pdfPreviewRenderer;
            _fileInteractionService = fileInteractionService;
            _logger = logger;
            Pages = [];
            LoadCommand = new AsyncCommand(LoadAsync, LogUnhandledCommandException, () => !IsBusy && !_didLoad);
            ShareCommand = new AsyncCommand(ShareAsync, LogUnhandledCommandException, () => !IsBusy);
            OpenExternallyCommand = new AsyncCommand(OpenExternallyAsync, LogUnhandledCommandException, () => !IsBusy);
        }

        public string Title { get; }

        public string Details { get; }

        public RangeObservableCollection<PdfPreviewPageSnapshot> Pages { get; }

        public AsyncCommand LoadCommand { get; }

        public AsyncCommand ShareCommand { get; }

        public AsyncCommand OpenExternallyCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsPreviewVisible));
                    OnPropertyChanged(nameof(IsEmptyVisible));
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

        public bool IsPreviewVisible => Pages.Count > 0;

        public bool IsEmptyVisible => _didLoad && Pages.Count == 0 && !IsBusy;

        public string EmptyText => "PDF preview unavailable";

        private async Task LoadAsync()
        {
            if (IsBusy || _didLoad)
            {
                return;
            }

            IsBusy = true;
            Status = "Rendering PDF...";
            try
            {
                PdfPreviewDocumentSnapshot document = await _pdfPreviewRenderer.RenderAsync(_file.FilePath);
                Pages.ReplaceWith(document.Pages);

                _didLoad = true;
                Status = document.HasPages ? document.StatusText : EmptyText;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to render PDF preview for {FilePath}.", _file.FilePath);
                _didLoad = true;
                Status = ViewerFileActionFailureStatus.Create(exception, EmptyText);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(IsPreviewVisible));
                OnPropertyChanged(nameof(IsEmptyVisible));
                LoadCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task ShareAsync()
        {
            await RunViewerActionAsync(
                null,
                () => _fileInteractionService.ShareAsync(_file),
                "Share failed.",
                "Failed to share PDF viewer file {FilePath}.");
        }

        private async Task OpenExternallyAsync()
        {
            await RunViewerActionAsync(
                null,
                () => _fileInteractionService.OpenAsync(_file),
                "Open failed.",
                "Failed to open PDF viewer file {FilePath}.");
        }

        private async Task RunViewerActionAsync(
            string? busyStatus,
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
            LoadCommand.RaiseCanExecuteChanged();
            ShareCommand.RaiseCanExecuteChanged();
            OpenExternallyCommand.RaiseCanExecuteChanged();
        }

        private void LogUnhandledCommandException(Exception exception)
        {
            _logger.LogError(exception, "Unhandled Cotton mobile PDF viewer command exception.");
        }
    }
}
