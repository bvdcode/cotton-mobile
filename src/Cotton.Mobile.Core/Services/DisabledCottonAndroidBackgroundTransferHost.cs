namespace Cotton.Mobile.Services
{
    public sealed class DisabledCottonAndroidBackgroundTransferHost : ICottonAndroidBackgroundTransferHost
    {
        public static DisabledCottonAndroidBackgroundTransferHost Instance { get; } = new();

        private DisabledCottonAndroidBackgroundTransferHost()
        {
        }

        public Task<CottonAndroidBackgroundTransferScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundTransferRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return Task.FromResult(
                CottonAndroidBackgroundTransferScheduleResult.Unsupported(
                    request,
                    "Android background transfer host is not available yet."));
        }
    }
}
