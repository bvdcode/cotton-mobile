using Cotton.Auth;
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cotton.Mobile.Tests
{
    internal static class TokenStorageTestData
    {
        public const string PairKey = "Cotton.Mobile.Auth.TokenPair";
        public const string LegacyAccessKey = "Cotton.Mobile.Auth.AccessToken";
        public const string LegacyRefreshKey = "Cotton.Mobile.Auth.RefreshToken";
        public const string NewRecord = "{\"accessToken\":\"new-access\",\"refreshToken\":\"new-refresh\"}";

        public static TokenPairDto NewPair()
        {
            return new TokenPairDto { AccessToken = "new-access", RefreshToken = "new-refresh" };
        }

        public static TestSecureStorage LegacyStorage()
        {
            TestSecureStorage storage = new();
            storage.Values[LegacyAccessKey] = "old-access";
            storage.Values[LegacyRefreshKey] = "old-refresh";
            return storage;
        }

        public static SecureStorageCottonTokenStore Create(TestSecureStorage storage)
        {
            return new SecureStorageCottonTokenStore(storage, NullLogger<SecureStorageCottonTokenStore>.Instance);
        }
    }
}
