namespace Cotton.Mobile.Services
{
    public interface ICottonAppLockCapabilityService
    {
        Task<CottonAppLockCapabilitySnapshot> GetCapabilityAsync(CancellationToken cancellationToken = default);
    }
}
