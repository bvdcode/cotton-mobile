namespace Cotton.Mobile.Services
{
    public interface ICloudShareLinkInteractionService
    {
        Task CopyAsync(
            CottonCloudShareLinkSnapshot link,
            CancellationToken cancellationToken = default);

        Task CopyAsync(
            IReadOnlyList<CottonCloudShareLinkSnapshot> links,
            CancellationToken cancellationToken = default);

        Task ShareAsync(
            CottonCloudShareLinkSnapshot link,
            string title,
            CancellationToken cancellationToken = default);

        Task ShareAsync(
            IReadOnlyList<CottonCloudShareLinkSnapshot> links,
            string title,
            CancellationToken cancellationToken = default);
    }
}
