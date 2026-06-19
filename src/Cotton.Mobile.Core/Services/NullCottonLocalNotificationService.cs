namespace Cotton.Mobile.Services
{
    public sealed class NullCottonLocalNotificationService : ICottonLocalNotificationService
    {
        public static NullCottonLocalNotificationService Instance { get; } = new();

        private NullCottonLocalNotificationService()
        {
        }

        public Task<CottonLocalNotificationDeliveryResult> ShowAsync(
            CottonLocalNotificationSnapshot notification,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CottonLocalNotificationDeliveryResult.Skipped);
        }
    }
}
