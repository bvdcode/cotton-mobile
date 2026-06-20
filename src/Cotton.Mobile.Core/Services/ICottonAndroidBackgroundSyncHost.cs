namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundSyncHost
    {
        Task<CottonAndroidBackgroundSyncScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundSyncRequest request,
            CancellationToken cancellationToken = default);
    }
}
