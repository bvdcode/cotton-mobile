namespace Cotton.Mobile.Services
{
    public interface IBackupSetupPageService
    {
        Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default);
    }
}
