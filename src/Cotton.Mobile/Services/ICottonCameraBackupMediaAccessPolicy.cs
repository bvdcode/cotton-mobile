namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMediaAccessPolicy
    {
        Task<bool> CanReadMediaAsync(CancellationToken cancellationToken = default);
    }
}
