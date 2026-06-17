using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

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
                && file.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
        }

        public async Task OpenAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(downloadedFile);

            cancellationToken.ThrowIfCancellationRequested();

            string details = CreateDetails(file, downloadedFile);
            var viewModel = ActivatorUtilities.CreateInstance<ImageViewerViewModel>(
                _serviceProvider,
                file.Name,
                details,
                downloadedFile);
            var page = ActivatorUtilities.CreateInstance<ImageViewerPage>(_serviceProvider, viewModel);
            await MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.Navigation.PushAsync(page));
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
