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

        Task<CottonFileBrowserEntry> UpdateContentAsync(
            Uri instanceUri,
            Guid fileId,
            CottonFolderHandle folder,
            string expectedETag,
            CottonFileUploadSource source,
            IProgress<long>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
