namespace Cotton.Mobile.Services
{
    public interface ICottonFileBrowserService
    {
        Task<CottonFolderContent> GetRootAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task<CottonFolderContent> GetFolderAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default);

        Task<CottonFileDownloadResult> DownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default);

        CottonLocalFileSnapshot? GetLocalDownload(CottonFileBrowserEntry file);

        CottonFileDownloadResult? GetReusableLocalDownload(CottonFileBrowserEntry file);
    }
}
