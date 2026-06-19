namespace Cotton.Mobile.Services
{
    public interface ICloudShareLinkInteractionService
    {
        Task CopyAsync(
            CottonCloudShareLinkSnapshot link,
            CancellationToken cancellationToken = default);

        Task ShareAsync(
            CottonCloudShareLinkSnapshot link,
            string title,
            CancellationToken cancellationToken = default);
    }
}
