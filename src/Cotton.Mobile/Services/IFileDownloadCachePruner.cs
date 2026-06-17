namespace Cotton.Mobile.Services
{
    public interface IFileDownloadCachePruner
    {
        Task PruneAsync(string? protectedPath = null, CancellationToken cancellationToken = default);
    }
}
