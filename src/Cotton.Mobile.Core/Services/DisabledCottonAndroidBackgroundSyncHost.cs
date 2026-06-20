namespace Cotton.Mobile.Services
{
    public class DisabledCottonAndroidBackgroundSyncHost : ICottonAndroidBackgroundSyncHost
    {
        public static DisabledCottonAndroidBackgroundSyncHost Instance { get; } = new();

        public Task<CottonAndroidBackgroundSyncScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundSyncRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(CottonAndroidBackgroundSyncScheduleResult.Unsupported(
                request,
                "Android background sync is unavailable on this platform."));
        }
    }
}
