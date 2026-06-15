namespace Cotton.Mobile.Services
{
    public interface ICottonInstanceStore
    {
        Task<Uri?> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
