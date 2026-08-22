using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class TestPermissionResolver : ICottonSyncLocalRootPermissionResolver
    {
        public CottonSyncRootPermissionStatus PermissionStatus { get; set; } =
            CottonSyncRootPermissionStatus.Available;

        public CottonSyncRootPermissionStatus Resolve(CottonSyncLocalRootSnapshot localRoot)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            return PermissionStatus;
        }
    }
}
