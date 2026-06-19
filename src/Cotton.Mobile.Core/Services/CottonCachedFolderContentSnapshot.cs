namespace Cotton.Mobile.Services
{
    public class CottonCachedFolderContentSnapshot
    {
        public CottonCachedFolderContentSnapshot(
            CottonFolderContent content,
            DateTime cachedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(content);

            Content = content;
            CachedAtUtc = CottonLocalFileFreshness.NormalizeUtc(cachedAtUtc);
        }

        public CottonFolderContent Content { get; }

        public DateTime CachedAtUtc { get; }
    }
}
