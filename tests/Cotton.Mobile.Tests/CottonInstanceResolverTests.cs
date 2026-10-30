using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonInstanceResolverTests
    {
        [Fact]
        public async Task EmptyAddressIsRejectedWithoutProbe()
        {
            StubCottonInstanceProbe probe = new(_ => true);
            CottonInstanceResolver resolver = new(probe);

            Uri? result = await resolver.ResolveAsync("   ");

            Assert.Null(result);
            Assert.Empty(probe.ProbedUris);
        }

        [Theory]
        [InlineData("https://cloud.example.test", "https://cloud.example.test/")]
        [InlineData("http://cloud.example.test:8080/base", "http://cloud.example.test:8080/base")]
        public async Task ExplicitHttpSchemeIsPreservedWithoutProbe(string address, string expected)
        {
            StubCottonInstanceProbe probe = new(_ => false);
            CottonInstanceResolver resolver = new(probe);

            Uri? result = await resolver.ResolveAsync(address);

            Assert.Equal(expected, result?.AbsoluteUri);
            Assert.Empty(probe.ProbedUris);
        }

        [Theory]
        [InlineData("ftp://cloud.example.test")]
        [InlineData("https://user@cloud.example.test")]
        [InlineData("https://cloud.example.test?query=value")]
        public async Task UnsupportedExplicitAddressIsRejected(string address)
        {
            StubCottonInstanceProbe probe = new(_ => true);
            CottonInstanceResolver resolver = new(probe);

            Uri? result = await resolver.ResolveAsync(address);

            Assert.Null(result);
            Assert.Empty(probe.ProbedUris);
        }

        [Fact]
        public async Task BareAddressUsesHttpsWhenCottonResponds()
        {
            StubCottonInstanceProbe probe = new(uri => uri.Scheme == Uri.UriSchemeHttps);
            CottonInstanceResolver resolver = new(probe);

            Uri? result = await resolver.ResolveAsync("cloud.example.test:8443/base");

            Assert.Equal("https://cloud.example.test:8443/base", result?.AbsoluteUri);
            Uri probedUri = Assert.Single(probe.ProbedUris);
            Assert.Equal("https://cloud.example.test:8443/base", probedUri.AbsoluteUri);
        }

        [Fact]
        public async Task BareAddressFallsBackToHttpAfterHttpsFails()
        {
            StubCottonInstanceProbe probe = new(uri => uri.Scheme == Uri.UriSchemeHttp);
            CottonInstanceResolver resolver = new(probe);

            Uri? result = await resolver.ResolveAsync("cloud.example.test");

            Assert.Equal("http://cloud.example.test/", result?.AbsoluteUri);
            Assert.Collection(
                probe.ProbedUris,
                uri => Assert.Equal("https://cloud.example.test/", uri.AbsoluteUri),
                uri => Assert.Equal("http://cloud.example.test/", uri.AbsoluteUri));
        }

        [Fact]
        public async Task BareAddressIsRejectedWhenNeitherProtocolIsCotton()
        {
            StubCottonInstanceProbe probe = new(_ => false);
            CottonInstanceResolver resolver = new(probe);

            Uri? result = await resolver.ResolveAsync("cloud.example.test");

            Assert.Null(result);
            Assert.Equal(2, probe.ProbedUris.Count);
        }
    }
}
