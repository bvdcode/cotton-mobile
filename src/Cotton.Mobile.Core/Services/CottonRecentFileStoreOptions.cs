namespace Cotton.Mobile.Services
{
    public class CottonRecentFileStoreOptions
    {
        public CottonRecentFileStoreOptions(int maxItems)
        {
            if (maxItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "Recent file limit must be positive.");
            }

            MaxItems = maxItems;
        }

        public int MaxItems { get; }

        public static CottonRecentFileStoreOptions Default { get; } = new(50);
    }
}
