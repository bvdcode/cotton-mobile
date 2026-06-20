namespace Cotton.Mobile.Services
{
    public interface IDeviceStorageSpaceService
    {
        Task<CottonDeviceStorageSpaceSnapshot> GetAppDataStorageSpaceAsync(
            CancellationToken cancellationToken = default);
    }
}
