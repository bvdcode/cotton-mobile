namespace Cotton.Mobile.Services
{
    public interface IFileThumbnailCache
    {
        string? GetCachedThumbnailSource(string cacheKey);

        Task WarmAsync(
            string cacheKey,
            Uri sourceUri,
            CancellationToken cancellationToken = default);
    }
}
