namespace Cotton.Mobile.Services
{
    public interface ICottonWindowPrivacyService
    {
        Task ApplyAsync(CancellationToken cancellationToken = default);
    }
}
