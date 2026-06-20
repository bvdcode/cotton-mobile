namespace Cotton.Mobile.Services
{
    public interface ICottonCurrentSessionRevocationHandler
    {
        Task RevokeCurrentSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default);
    }
}
