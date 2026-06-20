namespace Cotton.Mobile.Services
{
    public class DisabledCottonWindowPrivacyService : ICottonWindowPrivacyService
    {
        public Task ApplyAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }
}
