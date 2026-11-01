using Cotton.Auth;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SecureStorageCottonTokenMigrationTests
    {
        [Fact]
        public async Task ExistingPairIsMigratedOnceWithoutChangingTokens()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            TokenPairDto? migrated = await store.GetAsync(cancellationToken);
            TokenPairDto? reopened = await store.GetAsync(cancellationToken);

            Assert.NotNull(migrated);
            Assert.NotNull(reopened);
            Assert.Equal("old-access", migrated.AccessToken);
            Assert.Equal("old-refresh", migrated.RefreshToken);
            Assert.Equal(migrated.AccessToken, reopened.AccessToken);
            Assert.Equal(migrated.RefreshToken, reopened.RefreshToken);
            Assert.Equal(TokenStorageTestData.PairKey, Assert.Single(storage.Values).Key);
            Assert.Single(storage.Writes);
        }

        [Fact]
        public async Task FailedMigrationDoesNotDeleteExistingTokens()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            storage.FailWrite = true;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAsync<IOException>(() => store.GetAsync(cancellationToken));

            Assert.Equal("old-access", storage.Values[TokenStorageTestData.LegacyAccessKey]);
            Assert.Equal("old-refresh", storage.Values[TokenStorageTestData.LegacyRefreshKey]);
            Assert.Empty(storage.Removes);
        }

        [Fact]
        public async Task InterruptedCleanupKeepsTheNewRecordAuthoritative()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            storage.FailedRemovalKey = TokenStorageTestData.LegacyAccessKey;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetAsync(cancellationToken));
            TokenPairDto? tokens = await store.GetAsync(cancellationToken);

            Assert.NotNull(tokens);
            Assert.Equal("old-access", tokens.AccessToken);
            Assert.Equal("old-refresh", tokens.RefreshToken);
            Assert.Single(storage.Writes);
        }

        [Theory]
        [InlineData("Cotton.Mobile.Auth.AccessToken")]
        [InlineData("Cotton.Mobile.Auth.RefreshToken")]
        public async Task IncompleteLegacyPairIsNotMigrated(string key)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = new();
            storage.Values[key] = "remaining-value";
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            Assert.Null(await store.GetAsync(cancellationToken));

            Assert.Empty(storage.Writes);
            Assert.Empty(storage.Values);
        }

        [Fact]
        public async Task MissingTokensDoNotCreateARecord()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = new();
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            Assert.Null(await store.GetAsync(cancellationToken));

            Assert.Empty(storage.Writes);
        }

        [Fact]
        public async Task MigrationCannotOverwriteANewerSave()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            TaskCompletionSource reachedWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource releaseWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
            storage.BeforeWrite = async () =>
            {
                reachedWrite.TrySetResult();
                await releaseWrite.Task;
            };
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            Task<TokenPairDto?> migration = store.GetAsync(cancellationToken);
            await reachedWrite.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            Task save = store.SaveAsync(TokenStorageTestData.NewPair(), cancellationToken);
            Assert.False(save.IsCompleted);
            releaseWrite.SetResult();
            await Task.WhenAll(migration, save);

            TokenPairDto? tokens = await store.GetAsync(cancellationToken);
            Assert.NotNull(tokens);
            Assert.Equal("new-access", tokens.AccessToken);
            Assert.Equal("new-refresh", tokens.RefreshToken);
        }

        [Fact]
        public async Task MigrationCannotRestoreTokensAfterClear()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            TaskCompletionSource reachedWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource releaseWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
            storage.BeforeWrite = async () =>
            {
                reachedWrite.TrySetResult();
                await releaseWrite.Task;
            };
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            Task<TokenPairDto?> migration = store.GetAsync(cancellationToken);
            await reachedWrite.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            Task clear = store.ClearAsync(cancellationToken);
            Assert.False(clear.IsCompleted);
            releaseWrite.SetResult();
            await Task.WhenAll(migration, clear);

            Assert.Null(await store.GetAsync(cancellationToken));
            Assert.Empty(storage.Values);
        }
    }
}
