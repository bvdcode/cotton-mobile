using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AvatarUrlTests
    {
        private static readonly Uri Instance = new("https://app.cottoncloud.dev");

        [Fact]
        public void EncryptedHashBecomesAPreviewUrl()
        {
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "abc123");

            Assert.NotNull(url);
            Assert.Equal("https://app.cottoncloud.dev/api/v1/preview/abc123.webp", url.AbsoluteUri);
        }

        [Fact]
        public void UrlMatchesTheWebClientForTheSameHash()
        {
            // cotton/src/cotton.client/src/shared/api/authApi.test.ts expects the same path
            // for this input, so both clients must agree on escaping and extension.
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "hash value");

            Assert.NotNull(url);
            Assert.Equal("/api/v1/preview/hash%20value.webp", url.PathAndQuery);
        }

        [Fact]
        public void HashIsEscapedSoItCannotBreakOutOfThePath()
        {
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "hash value/../evil");

            Assert.NotNull(url);
            Assert.Equal(
                "https://app.cottoncloud.dev/api/v1/preview/hash%20value%2F..%2Fevil.webp",
                url.AbsoluteUri);
        }

        [Fact]
        public void SurroundingWhitespaceIsTrimmed()
        {
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "  abc123  ");

            Assert.NotNull(url);
            Assert.Equal("https://app.cottoncloud.dev/api/v1/preview/abc123.webp", url.AbsoluteUri);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AccountsWithoutAnAvatarProduceNoUrl(string? avatarHashEncryptedHex)
        {
            Assert.Null(CottonAvatarUrl.TryCreate(Instance, avatarHashEncryptedHex));
        }

        [Fact]
        public void InstancePortIsPreserved()
        {
            Uri? url = CottonAvatarUrl.TryCreate(new Uri("https://cotton.local:8443"), "abc123");

            Assert.NotNull(url);
            Assert.Equal("https://cotton.local:8443/api/v1/preview/abc123.webp", url.AbsoluteUri);
        }

        [Fact]
        public void MissingInstanceIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => CottonAvatarUrl.TryCreate(null!, "abc123"));
        }
    }
}
