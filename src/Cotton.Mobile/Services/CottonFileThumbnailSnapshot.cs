namespace Cotton.Mobile.Services
{
    public class CottonFileThumbnailSnapshot
    {
        private CottonFileThumbnailSnapshot(
            CottonFileThumbnailState state,
            string placeholderText,
            string? source,
            string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("Thumbnail cache key is required.", nameof(cacheKey));
            }

            State = state;
            PlaceholderText = string.IsNullOrWhiteSpace(placeholderText)
                ? "FILE"
                : placeholderText.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
            CacheKey = cacheKey.Trim();
        }

        public CottonFileThumbnailState State { get; }

        public string CacheKey { get; }

        public string PlaceholderText { get; }

        public string? Source { get; }

        public bool IsLoading => State == CottonFileThumbnailState.Loading;

        public bool HasImage => State == CottonFileThumbnailState.Ready
            && !string.IsNullOrWhiteSpace(Source);

        public bool IsPlaceholderVisible => !HasImage && !IsLoading;

        public static CottonFileThumbnailSnapshot Placeholder(string placeholderText, string cacheKey)
        {
            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Placeholder,
                placeholderText,
                null,
                cacheKey);
        }

        public static CottonFileThumbnailSnapshot Loading(string placeholderText, string cacheKey)
        {
            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Loading,
                placeholderText,
                null,
                cacheKey);
        }

        public static CottonFileThumbnailSnapshot Ready(string placeholderText, string source, string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Thumbnail source is required.", nameof(source));
            }

            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Ready,
                placeholderText,
                source,
                cacheKey);
        }

        public static CottonFileThumbnailSnapshot Failed(string placeholderText, string cacheKey)
        {
            return new CottonFileThumbnailSnapshot(
                CottonFileThumbnailState.Failed,
                placeholderText,
                null,
                cacheKey);
        }
    }
}
