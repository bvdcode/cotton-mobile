using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile.Services
{
    public interface ICottonProfileCacheStore
    {
        Task<MainPageProfile?> GetAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task SaveAsync(Uri instanceUri, MainPageProfile profile, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
