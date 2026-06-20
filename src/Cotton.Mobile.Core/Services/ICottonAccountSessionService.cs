namespace Cotton.Mobile.Services
{
    public interface ICottonAccountSessionService
    {
        Task<IReadOnlyList<CottonAccountSessionSnapshot>> GetActiveSessionsAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
