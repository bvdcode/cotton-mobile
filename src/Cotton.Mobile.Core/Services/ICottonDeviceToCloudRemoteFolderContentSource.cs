namespace Cotton.Mobile.Services
{
    public interface ICottonDeviceToCloudRemoteFolderContentSource
    {
        Task<CottonFolderContent> LoadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default);
    }
}
