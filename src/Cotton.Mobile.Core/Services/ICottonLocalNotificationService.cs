namespace Cotton.Mobile.Services
{
    public interface ICottonLocalNotificationService
    {
        Task<CottonLocalNotificationDeliveryResult> ShowAsync(
            CottonLocalNotificationSnapshot notification,
            CancellationToken cancellationToken = default);
    }
}
