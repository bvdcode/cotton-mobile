namespace Cotton.Mobile.Services
{
    public interface ICottonAndroidBackgroundTransferHost
    {
        Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundTransferRequest request,
            CancellationToken cancellationToken = default);
    }
}
