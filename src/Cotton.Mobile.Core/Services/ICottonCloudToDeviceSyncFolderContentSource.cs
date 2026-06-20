namespace Cotton.Mobile.Services
{
    public interface ICottonCloudToDeviceSyncFolderContentSource
    {
        Task<CottonFolderContent> LoadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default);
    }
}
