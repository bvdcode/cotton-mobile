using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class AvatarUrlTests
    {
        private static readonly Uri Instance = new("https://app.cottoncloud.dev");

        [Fact]
        public void Encrypted_hash_becomes_a_preview_url()
        {
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "abc123");

            Assert.NotNull(url);
            Assert.Equal("https://app.cottoncloud.dev/api/v1/preview/abc123.webp", url.AbsoluteUri);
        }

        [Fact]
        public void Url_matches_the_web_client_for_the_same_hash()
        {
            // cotton/src/cotton.client/src/shared/api/authApi.test.ts expects the same path
            // for this input, so both clients must agree on escaping and extension.
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "hash value");

            Assert.NotNull(url);
            Assert.Equal("/api/v1/preview/hash%20value.webp", url.PathAndQuery);
        }

        [Fact]
        public void Hash_is_escaped_so_it_cannot_break_out_of_the_path()
        {
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "hash value/../evil");

            Assert.NotNull(url);
            Assert.Equal(
                "https://app.cottoncloud.dev/api/v1/preview/hash%20value%2F..%2Fevil.webp",
                url.AbsoluteUri);
        }

        [Fact]
        public void Surrounding_whitespace_is_trimmed()
        {
            Uri? url = CottonAvatarUrl.TryCreate(Instance, "  abc123  ");

            Assert.NotNull(url);
            Assert.Equal("https://app.cottoncloud.dev/api/v1/preview/abc123.webp", url.AbsoluteUri);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Accounts_without_an_avatar_produce_no_url(string? avatarHashEncryptedHex)
        {
            Assert.Null(CottonAvatarUrl.TryCreate(Instance, avatarHashEncryptedHex));
        }

        [Fact]
        public void Instance_port_is_preserved()
        {
            Uri? url = CottonAvatarUrl.TryCreate(new Uri("https://cotton.local:8443"), "abc123");

            Assert.NotNull(url);
            Assert.Equal("https://cotton.local:8443/api/v1/preview/abc123.webp", url.AbsoluteUri);
        }

        [Fact]
        public void Missing_instance_is_rejected()
        {
            Assert.Throws<ArgumentNullException>(() => CottonAvatarUrl.TryCreate(null!, "abc123"));
        }
    }
}
