namespace Cotton.Mobile.Services
{
    public interface ICottonPendingAppCodeSessionStore
    {
        Task<CottonPendingAppCodeSession?> GetAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(CottonPendingAppCodeSession session, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
