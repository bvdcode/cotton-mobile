// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using System.Text;

namespace Cotton.Mobile.Services
{
    public class FilePreviewService : IFilePreviewService
    {
        private readonly IServiceProvider _serviceProvider;

        public FilePreviewService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public bool CanPreview(CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return file.Type == CottonFileBrowserEntryType.File
                && CottonFileOpenRouter.CreateRoute(file).CanPreviewInApp;
        }

        public bool CanPreview(CottonFileBrowserEntry file, CottonFileDownloadResult downloadedFile)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(downloadedFile);

            return file.Type == CottonFileBrowserEntryType.File
                && CottonFileOpenRouter.CreateRoute(file, downloadedFile.SizeBytes).CanPreviewInApp;
        }

        public bool CanPreview(CottonFileBrowserEntry file, CottonLocalFileSnapshot localFile)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(localFile);

            return file.Type == CottonFileBrowserEntryType.File
                && CottonFileOpenRouter.CreateRoute(file, localFile.SizeBytes).CanPreviewInApp;
        }

        public async Task OpenAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(downloadedFile);

            cancellationToken.ThrowIfCancellationRequested();
            EnsureFileExists(downloadedFile);

            CottonFileOpenRoute route = CottonFileOpenRouter.CreateRoute(file, downloadedFile.SizeBytes);
            if (route.PreviewKind == CottonFilePreviewKind.Image)
            {
                await OpenImagePreviewAsync(file, downloadedFile, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (route.PreviewKind is CottonFilePreviewKind.Audio or CottonFilePreviewKind.Video)
            {
                await OpenMediaPreviewAsync(file, downloadedFile, route.PreviewKind, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            if (route.PreviewKind == CottonFilePreviewKind.Pdf)
            {
                await OpenPdfPreviewAsync(file, downloadedFile, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (route.PreviewKind != CottonFilePreviewKind.Text)
            {
                throw new InvalidOperationException("The selected file cannot be previewed inside Cotton.");
            }

            string textContent = await ReadTextPreviewAsync(file, downloadedFile, cancellationToken).ConfigureAwait(false);
            await OpenTextPreviewAsync(file, downloadedFile, textContent, cancellationToken).ConfigureAwait(false);
        }

        private async Task OpenImagePreviewAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ContentPage page = CreateImageViewerPage(file, downloadedFile);
                await CottonShellNavigation.PushAsync(page, cancellationToken);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task OpenMediaPreviewAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CottonFilePreviewKind previewKind,
            CancellationToken cancellationToken)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ContentPage page = CreateMediaViewerPage(file, downloadedFile, previewKind);
                await CottonShellNavigation.PushAsync(page, cancellationToken);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task OpenPdfPreviewAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ContentPage page = CreatePdfViewerPage(file, downloadedFile);
                await CottonShellNavigation.PushAsync(page, cancellationToken);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task OpenTextPreviewAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            string content,
            CancellationToken cancellationToken)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ContentPage page = CreateTextViewerPage(file, downloadedFile, content);
                await CottonShellNavigation.PushAsync(page, cancellationToken);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        private ImageViewerPage CreateImageViewerPage(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile)
        {
            string details = CreateDetails(file);
            var viewModel = ActivatorUtilities.CreateInstance<ImageViewerViewModel>(
                _serviceProvider,
                file.Name,
                details,
                downloadedFile);
            return ActivatorUtilities.CreateInstance<ImageViewerPage>(_serviceProvider, viewModel);
        }

        private MediaViewerPage CreateMediaViewerPage(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CottonFilePreviewKind previewKind)
        {
            string details = CreateDetails(file);
            string? videoPosterSource = previewKind == CottonFilePreviewKind.Video && file.Thumbnail.HasImage
                ? file.Thumbnail.Source
                : null;
            var viewModel = ActivatorUtilities.CreateInstance<MediaViewerViewModel>(
                _serviceProvider,
                file.Name,
                details,
                previewKind,
                videoPosterSource ?? string.Empty,
                downloadedFile);
            return ActivatorUtilities.CreateInstance<MediaViewerPage>(_serviceProvider, viewModel);
        }

        private PdfViewerPage CreatePdfViewerPage(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile)
        {
            string details = CreateDetails(file);
            var viewModel = ActivatorUtilities.CreateInstance<PdfViewerViewModel>(
                _serviceProvider,
                file.Name,
                details,
                downloadedFile);
            return ActivatorUtilities.CreateInstance<PdfViewerPage>(_serviceProvider, viewModel);
        }

        private async Task<string> ReadTextPreviewAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            if (CottonFileOpenRouter.CreateRoute(file, downloadedFile.SizeBytes).PreviewKind != CottonFilePreviewKind.Text)
            {
                throw new InvalidOperationException("The selected file cannot be previewed as text.");
            }

            return await File.ReadAllTextAsync(
                downloadedFile.FilePath,
                Encoding.UTF8,
                cancellationToken).ConfigureAwait(false);
        }

        private TextViewerPage CreateTextViewerPage(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            string content)
        {
            CottonTextPreviewDisplayState display =
                CottonTextPreviewDisplayState.Create(file.Kind, downloadedFile.SizeBytes, content);
            var viewModel = ActivatorUtilities.CreateInstance<TextViewerViewModel>(
                _serviceProvider,
                file.Name,
                display.DetailsText,
                content,
                downloadedFile);
            return ActivatorUtilities.CreateInstance<TextViewerPage>(_serviceProvider, viewModel);
        }

        private static string CreateDetails(CottonFileBrowserEntry file)
        {
            return string.IsNullOrWhiteSpace(file.Kind) ? "File" : file.Kind.Trim();
        }

        private static void EnsureFileExists(CottonFileDownloadResult file)
        {
            if (!File.Exists(file.FilePath))
            {
                throw new FileNotFoundException("The local preview file is no longer available.", file.FilePath);
            }
        }
    }
}
