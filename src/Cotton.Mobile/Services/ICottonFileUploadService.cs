namespace Cotton.Mobile.Services
{
    public interface ICottonFileUploadService
    {
        Task<CottonFileBrowserEntry> UploadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CottonFileUploadSource source,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
