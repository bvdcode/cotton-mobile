namespace Cotton.Mobile.Services
{
    public class CottonLogoutCacheCleanupSettings
    {
        public CottonLogoutCacheCleanupSettings(bool clearCachedFilesOnLogout)
        {
            ClearCachedFilesOnLogout = clearCachedFilesOnLogout;
        }

        public bool ClearCachedFilesOnLogout { get; }

        public static CottonLogoutCacheCleanupSettings Default { get; } = new(true);

        public CottonLogoutCacheCleanupSettings WithClearCachedFilesOnLogout(bool clearCachedFilesOnLogout)
        {
            return ClearCachedFilesOnLogout == clearCachedFilesOnLogout
                ? this
                : new CottonLogoutCacheCleanupSettings(clearCachedFilesOnLogout);
        }
    }
}
