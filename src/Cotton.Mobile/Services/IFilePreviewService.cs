namespace Cotton.Mobile.Services
{
    public interface IFilePreviewService
    {
        bool CanPreview(CottonFileBrowserEntry file);

        bool CanPreview(CottonFileBrowserEntry file, CottonFileDownloadResult downloadedFile);

        Task OpenAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken = default);
    }
}
