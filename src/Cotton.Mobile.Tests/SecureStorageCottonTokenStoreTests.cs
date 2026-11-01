using System.Text.Json;
using Cotton.Auth;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SecureStorageCottonTokenStoreTests
    {
        [Fact]
        public async Task SaveWritesBothTokensInOneRecord()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = new();
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await store.SaveAsync(TokenStorageTestData.NewPair(), cancellationToken);

            Assert.Equal(TokenStorageTestData.PairKey, Assert.Single(storage.Writes));
            Assert.Single(storage.Values);
            using SecureStorageCottonTokenStore reopened = TokenStorageTestData.Create(storage);
            TokenPairDto? tokens = await reopened.GetAsync(cancellationToken);
            Assert.NotNull(tokens);
            Assert.Equal("new-access", tokens.AccessToken);
            Assert.Equal("new-refresh", tokens.RefreshToken);
        }

        [Fact]
        public async Task FailureAfterRecordWriteLeavesBothNewTokens()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            storage.AfterWrite = () => throw new IOException("Completion was interrupted.");
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(TokenStorageTestData.NewPair(), cancellationToken));

            using SecureStorageCottonTokenStore reopened = TokenStorageTestData.Create(storage);
            TokenPairDto? tokens = await reopened.GetAsync(cancellationToken);
            Assert.NotNull(tokens);
            Assert.Equal("new-access", tokens.AccessToken);
            Assert.Equal("new-refresh", tokens.RefreshToken);
            Assert.Empty(storage.Removes);
        }

        [Fact]
        public async Task FailedReplacementPreservesPreviousCompleteRecord()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = new();
            storage.Values[TokenStorageTestData.PairKey] = TokenStorageTestData.NewRecord;
            storage.FailWrite = true;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAsync<IOException>(() => store.SaveAsync(new TokenPairDto
            {
                AccessToken = "replacement-access",
                RefreshToken = "replacement-refresh",
            }, cancellationToken));

            Assert.Equal(TokenStorageTestData.NewRecord, storage.Values[TokenStorageTestData.PairKey]);
            Assert.Empty(storage.Removes);
        }

        [Fact]
        public async Task ReadFailurePreservesStoredRecord()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = new() { FailRead = true };
            storage.Values[TokenStorageTestData.PairKey] = TokenStorageTestData.NewRecord;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAsync<IOException>(() => store.GetAsync(cancellationToken));

            Assert.Equal(TokenStorageTestData.NewRecord, storage.Values[TokenStorageTestData.PairKey]);
            Assert.Empty(storage.Removes);
        }

        [Theory]
        [InlineData("")]
        [InlineData("{invalid")]
        public async Task MalformedRecordDoesNotRestoreLegacyTokens(string value)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            storage.Values[TokenStorageTestData.PairKey] = value;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAnyAsync<JsonException>(() => store.GetAsync(cancellationToken));

            Assert.Equal(TokenStorageTestData.PairKey, Assert.Single(storage.Reads));
            Assert.Equal(value, storage.Values[TokenStorageTestData.PairKey]);
            Assert.Empty(storage.Removes);
            Assert.Empty(storage.Writes);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("{\"accessToken\":\"new-access\"}")]
        [InlineData("{\"refreshToken\":\"new-refresh\"}")]
        public async Task IncompleteRecordDoesNotRestoreLegacyTokens(string value)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            storage.Values[TokenStorageTestData.PairKey] = value;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAsync<InvalidDataException>(() => store.GetAsync(cancellationToken));

            Assert.Equal(TokenStorageTestData.PairKey, Assert.Single(storage.Reads));
            Assert.Empty(storage.Removes);
            Assert.Empty(storage.Writes);
        }

        [Fact]
        public async Task CancellationBeforeSaveDoesNotWrite()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = new();
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                store.SaveAsync(TokenStorageTestData.NewPair(), cancellation.Token));

            Assert.Empty(storage.Values);
        }

        [Fact]
        public async Task CancellationAfterWritePreservesTheCompleteNewPair()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = new();
            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            storage.AfterWrite = cancellation.CancelAsync;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                store.SaveAsync(TokenStorageTestData.NewPair(), cancellation.Token));

            TokenPairDto? tokens = await store.GetAsync(cancellationToken);
            Assert.NotNull(tokens);
            Assert.Equal("new-access", tokens.AccessToken);
            Assert.Equal("new-refresh", tokens.RefreshToken);
            Assert.Single(storage.Writes);
        }

        [Fact]
        public async Task ClearRemovesBothFormatsButPreservesOtherSecureValues()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            storage.Values[TokenStorageTestData.PairKey] = TokenStorageTestData.NewRecord;
            storage.Values["unrelated"] = "preserved";
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await store.ClearAsync(cancellationToken);

            Assert.Null(await store.GetAsync(cancellationToken));
            Assert.Equal("preserved", storage.Values["unrelated"]);
            Assert.Single(storage.Values);
        }

        [Fact]
        public async Task FailedLegacyRemovalDoesNotExposeStaleTokensThroughClear()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TestSecureStorage storage = TokenStorageTestData.LegacyStorage();
            storage.Values[TokenStorageTestData.PairKey] = TokenStorageTestData.NewRecord;
            storage.FailedRemovalKey = TokenStorageTestData.LegacyAccessKey;
            using SecureStorageCottonTokenStore store = TokenStorageTestData.Create(storage);

            await Assert.ThrowsAsync<InvalidOperationException>(() => store.ClearAsync(cancellationToken));

            Assert.Equal(TokenStorageTestData.NewRecord, storage.Values[TokenStorageTestData.PairKey]);
            Assert.DoesNotContain(TokenStorageTestData.PairKey, storage.Removes);
        }
    }
}
