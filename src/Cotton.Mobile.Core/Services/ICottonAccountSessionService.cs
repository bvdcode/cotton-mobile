namespace Cotton.Mobile.Services
{
    public interface ICottonAccountSessionService
    {
        Task<IReadOnlyList<CottonAccountSessionSnapshot>> GetActiveSessionsAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task RevokeSessionAsync(
            Uri instanceUri,
            string sessionId,
            CancellationToken cancellationToken = default);
    }
}
