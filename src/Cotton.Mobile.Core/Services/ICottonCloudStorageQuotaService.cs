namespace Cotton.Mobile.Services
{
    public interface ICottonCloudStorageQuotaService
    {
        Task<CottonCloudStorageQuotaSnapshot> GetCurrentAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
