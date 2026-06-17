using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using System.Text;

namespace Cotton.Mobile.Services
{
    public class FilePreviewService : IFilePreviewService
    {
        private const long MaxTextPreviewBytes = 512 * 1024;

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
                && (file.IsImage || CanPreviewText(file));
        }

        public async Task OpenAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(downloadedFile);

            cancellationToken.ThrowIfCancellationRequested();

            ContentPage page = file.IsImage
                ? CreateImageViewerPage(file, downloadedFile)
                : await CreateTextViewerPageAsync(file, downloadedFile, cancellationToken).ConfigureAwait(false);
            await MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.Navigation.PushAsync(page));
        }

        private static bool CanPreviewText(CottonFileBrowserEntry file)
        {
            return file.IsText && file.SizeBytes is >= 0 and <= MaxTextPreviewBytes;
        }

        private ImageViewerPage CreateImageViewerPage(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile)
        {
            string details = CreateDetails(file, downloadedFile);
            var viewModel = ActivatorUtilities.CreateInstance<ImageViewerViewModel>(
                _serviceProvider,
                file.Name,
                details,
                downloadedFile);
            return ActivatorUtilities.CreateInstance<ImageViewerPage>(_serviceProvider, viewModel);
        }

        private async Task<TextViewerPage> CreateTextViewerPageAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken)
        {
            if (!CanPreviewText(file))
            {
                throw new InvalidOperationException("The selected file cannot be previewed as text.");
            }

            string content = await File.ReadAllTextAsync(
                downloadedFile.FilePath,
                Encoding.UTF8,
                cancellationToken).ConfigureAwait(false);
            string details = CreateDetails(file, downloadedFile);
            var viewModel = ActivatorUtilities.CreateInstance<TextViewerViewModel>(
                _serviceProvider,
                file.Name,
                details,
                content,
                downloadedFile);
            return ActivatorUtilities.CreateInstance<TextViewerPage>(_serviceProvider, viewModel);
        }

        private static string CreateDetails(CottonFileBrowserEntry file, CottonFileDownloadResult downloadedFile)
        {
            string size = FormatSize(downloadedFile.SizeBytes);
            return $"{file.Kind} · {size}";
        }

        private static string FormatSize(long bytes)
        {
            const long Kilobyte = 1024;
            const long Megabyte = Kilobyte * 1024;
            const long Gigabyte = Megabyte * 1024;

            return bytes switch
            {
                < Kilobyte => $"{bytes} B",
                < Megabyte => $"{bytes / (double)Kilobyte:0.#} KB",
                < Gigabyte => $"{bytes / (double)Megabyte:0.#} MB",
                _ => $"{bytes / (double)Gigabyte:0.#} GB",
            };
        }
    }
}
