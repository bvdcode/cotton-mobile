namespace Cotton.Mobile.Services
{
    public interface ICottonCloudShareLinkService
    {
        Task<CottonCloudShareLinkSnapshot> CreateAsync(
            Uri instanceUri,
            CottonCloudShareLinkRequest request,
            CancellationToken cancellationToken = default);

        Task InvalidateAllFileLinksAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);
    }
}
