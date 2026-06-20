namespace Cotton.Mobile.Services
{
    public interface ICottonFileBrowserService
    {
        Task<CottonFolderContent> GetRootAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task<CottonFolderContent> GetFolderAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default);

        Task<CottonFileBrowserEntry> CreateFolderAsync(
            Uri instanceUri,
            CottonFolderHandle parentFolder,
            string folderName,
            CancellationToken cancellationToken = default);

        Task<CottonFileDownloadResult> DownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default);

        CottonLocalFileSnapshot? GetLocalDownload(Uri instanceUri, CottonFileBrowserEntry file);

        CottonLocalFileSnapshot? GetReusableLocalDownloadSnapshot(Uri instanceUri, CottonFileBrowserEntry file);

        CottonFileDownloadResult? GetReusableLocalDownload(Uri instanceUri, CottonFileBrowserEntry file);

        Task<bool> DeleteLocalDownloadAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken = default);
    }
}
