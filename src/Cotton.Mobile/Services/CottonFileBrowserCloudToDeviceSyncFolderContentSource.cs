namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserCloudToDeviceSyncFolderContentSource :
        ICottonCloudToDeviceSyncFolderContentSource,
        ICottonDeviceToCloudRemoteFolderContentSource
    {
        private readonly ICottonFileBrowserService _fileBrowserService;

        public CottonFileBrowserCloudToDeviceSyncFolderContentSource(ICottonFileBrowserService fileBrowserService)
        {
            ArgumentNullException.ThrowIfNull(fileBrowserService);

            _fileBrowserService = fileBrowserService;
        }

        public Task<CottonFolderContent> LoadAsync(
            Uri instanceUri,
            CottonFolderHandle folder,
            CancellationToken cancellationToken = default)
        {
            return _fileBrowserService.GetFolderAsync(instanceUri, folder, cancellationToken);
        }
    }
}
