namespace Cotton.Mobile.Services
{
    public interface IFilePreviewService
    {
        bool CanPreview(CottonFileBrowserEntry file);

        Task OpenAsync(
            CottonFileBrowserEntry file,
            CottonFileDownloadResult downloadedFile,
            CancellationToken cancellationToken = default);
    }
}
