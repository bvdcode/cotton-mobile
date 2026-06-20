using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class DeviceStorageSpaceService : IDeviceStorageSpaceService
    {
        private readonly ILogger<DeviceStorageSpaceService> _logger;

        public DeviceStorageSpaceService(ILogger<DeviceStorageSpaceService> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            _logger = logger;
        }

        public Task<CottonDeviceStorageSpaceSnapshot> GetAppDataStorageSpaceAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                string appDataDirectory = FileSystem.AppDataDirectory;
                Directory.CreateDirectory(appDataDirectory);
                string? root = Path.GetPathRoot(Path.GetFullPath(appDataDirectory));
                if (string.IsNullOrWhiteSpace(root))
                {
                    return Task.FromResult(CottonDeviceStorageSpaceSnapshot.Unavailable(
                        "Free device space is unavailable."));
                }

                var drive = new DriveInfo(root);
                return Task.FromResult(CottonDeviceStorageSpaceSnapshot.Available(
                    drive.AvailableFreeSpace,
                    drive.TotalSize));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to inspect Cotton mobile app data free space.");
                return Task.FromResult(CottonDeviceStorageSpaceSnapshot.Unavailable(
                    "Free device space is unavailable."));
            }
        }
    }
}
