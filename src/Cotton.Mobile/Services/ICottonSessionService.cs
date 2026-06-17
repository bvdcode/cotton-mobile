namespace Cotton.Mobile.Services
{
    public interface ICottonSessionService
    {
        Task<CottonSessionResult> RestoreAsync(CancellationToken cancellationToken = default);

        Task<CottonSessionResult> SignInWithBrowserAsync(Uri instanceUri, CancellationToken cancellationToken = default);

        Task LogoutAsync(CancellationToken cancellationToken = default);

        Task ClearLocalSessionAsync(CancellationToken cancellationToken = default);
    }
}
