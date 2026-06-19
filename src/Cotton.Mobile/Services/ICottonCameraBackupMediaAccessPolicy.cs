namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMediaAccessPolicy
    {
        Task<CottonCameraBackupMediaAccessState> GetAccessStateAsync(
            CancellationToken cancellationToken = default);

        Task<CottonCameraBackupMediaAccessState> RequestAccessAsync(
            CancellationToken cancellationToken = default);

        Task OpenSettingsAsync(CancellationToken cancellationToken = default);

        Task<bool> CanReadMediaAsync(CancellationToken cancellationToken = default);
    }
}
