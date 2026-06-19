namespace Cotton.Mobile.Services
{
    public interface IFileDownloadCachePruner
    {
        Task PruneAsync(
            Uri instanceUri,
            string? protectedPath = null,
            CancellationToken cancellationToken = default);
    }
}
