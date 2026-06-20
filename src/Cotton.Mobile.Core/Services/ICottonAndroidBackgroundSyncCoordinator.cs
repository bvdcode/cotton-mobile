namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundSyncCoordinator
    {
        Task<CottonAndroidBackgroundSyncScheduleResult> ScheduleAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
