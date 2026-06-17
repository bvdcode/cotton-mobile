using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class FileThumbnailProvider : IFileThumbnailProvider
    {
        private readonly IFileThumbnailCache _thumbnailCache;
        private readonly ILogger<FileThumbnailProvider> _logger;

        public FileThumbnailProvider(
            IFileThumbnailCache thumbnailCache,
            ILogger<FileThumbnailProvider> logger)
        {
            ArgumentNullException.ThrowIfNull(thumbnailCache);
            ArgumentNullException.ThrowIfNull(logger);

            _thumbnailCache = thumbnailCache;
            _logger = logger;
        }

        public ValueTask<CottonFileThumbnailSnapshot> GetThumbnailAsync(
            Uri instanceUri,
            CottonFileBrowserEntry entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(entry);

            cancellationToken.ThrowIfCancellationRequested();
            CottonFileThumbnailSnapshot thumbnail = CreateThumbnail(instanceUri, entry);
            return ValueTask.FromResult(thumbnail);
        }

        private CottonFileThumbnailSnapshot CreateThumbnail(
            Uri instanceUri,
            CottonFileBrowserEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.PreviewHashEncryptedHex))
            {
                return CottonFileThumbnailSnapshot.Placeholder(
                    entry.BadgeText,
                    CreateCacheKey(entry, "no-preview"));
            }

            string previewToken = entry.PreviewHashEncryptedHex.Trim();
            string cacheKey = CreateCacheKey(entry, previewToken);
            string? cachedSource = _thumbnailCache.GetCachedThumbnailSource(cacheKey);
            if (!string.IsNullOrWhiteSpace(cachedSource))
            {
                return CottonFileThumbnailSnapshot.Ready(entry.BadgeText, cachedSource, cacheKey);
            }

            Uri previewUri = new(
                instanceUri,
                $"{Routes.V1.Previews}/{Uri.EscapeDataString(previewToken)}.webp");
            WarmCache(cacheKey, previewUri);
            return CottonFileThumbnailSnapshot.Ready(
                entry.BadgeText,
                previewUri.AbsoluteUri,
                cacheKey);
        }

        private void WarmCache(string cacheKey, Uri previewUri)
        {
            _ = WarmCacheAsync(cacheKey, previewUri);
        }

        private async Task WarmCacheAsync(string cacheKey, Uri previewUri)
        {
            try
            {
                await _thumbnailCache.WarmAsync(cacheKey, previewUri).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Thumbnail cache warm-up failed for {PreviewUri}.", previewUri);
            }
        }

        private static string CreateCacheKey(CottonFileBrowserEntry entry, string version)
        {
            return $"{entry.Type}:{entry.Id:N}:{version}";
        }
    }
}
