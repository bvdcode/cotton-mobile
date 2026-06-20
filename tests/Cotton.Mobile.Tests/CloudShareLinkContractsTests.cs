using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudShareLinkContractsTests
    {
        private static readonly Guid TargetId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Uri InstanceUri = new("https://cloud.example");

        [Fact]
        public void Current_capabilities_match_confirmed_backend_surface()
        {
            CottonCloudShareLinkCapabilitySnapshot capabilities = CottonCloudShareLinkCapabilitySnapshot.Current;

            Assert.True(capabilities.CanCreateLink(CottonCloudShareLinkTargetKind.File));
            Assert.True(capabilities.CanCreateLink(CottonCloudShareLinkTargetKind.Folder));
            Assert.True(capabilities.SupportsLinkExpiration);
            Assert.True(capabilities.SupportsGlobalInvalidate);
            Assert.True(capabilities.SupportsGlobalFileLinkInvalidate);
            Assert.False(capabilities.SupportsGlobalFolderLinkInvalidate);
            Assert.False(capabilities.SupportsPerLinkRevoke);
            Assert.False(capabilities.SupportsPassword);
            Assert.False(capabilities.SupportsSharePermissions);
            Assert.False(capabilities.SupportsActivityFeed);
            Assert.False(capabilities.SupportsSharedWithMe);
            Assert.False(capabilities.SupportsAccessRequests);
        }

        [Fact]
        public void Request_defaults_to_backend_link_lifetime()
        {
            CottonCloudShareLinkRequest request = CottonCloudShareLinkRequest.ForFile(TargetId);

            Assert.Equal(CottonCloudShareLinkTargetKind.File, request.TargetKind);
            Assert.Equal(TargetId, request.TargetId);
            Assert.Equal(CottonCloudShareLinkPolicy.DefaultExpireAfterMinutes, request.ExpireAfterMinutes);
        }

        [Fact]
        public void Expiration_options_match_supported_backend_lifetimes()
        {
            IReadOnlyList<CottonCloudShareLinkExpirationOption> options =
                CottonCloudShareLinkExpirationCatalog.CreateOptions();

            Assert.Equal(
                ["1 hour", "1 day", "7 days", "30 days", "1 year"],
                options.Select(option => option.Label).ToArray());
            Assert.Equal(
                [60, 1440, 10080, 43200, CottonCloudShareLinkPolicy.MaxExpireAfterMinutes],
                options.Select(option => option.ExpireAfterMinutes).ToArray());
            CottonCloudShareLinkExpirationOption defaultOption =
                Assert.Single(options, option => option.IsDefault);
            Assert.Equal(CottonCloudShareLinkPolicy.DefaultExpireAfterMinutes, defaultOption.ExpireAfterMinutes);
        }

        [Fact]
        public void Expiration_option_lookup_trims_label_and_rejects_unknown_values()
        {
            CottonCloudShareLinkExpirationOption? option =
                CottonCloudShareLinkExpirationCatalog.FindByLabel(" 30 days ");

            Assert.NotNull(option);
            Assert.Equal(43200, option.ExpireAfterMinutes);
            Assert.Null(CottonCloudShareLinkExpirationCatalog.FindByLabel("Never"));
        }

        [Fact]
        public void Request_supports_folder_targets()
        {
            CottonCloudShareLinkRequest request = CottonCloudShareLinkRequest.ForFolder(TargetId, expireAfterMinutes: 60);

            Assert.Equal(CottonCloudShareLinkTargetKind.Folder, request.TargetKind);
            Assert.Equal(TargetId, request.TargetId);
            Assert.Equal(60, request.ExpireAfterMinutes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(CottonCloudShareLinkPolicy.MaxExpireAfterMinutes + 1)]
        public void Request_rejects_unsupported_link_lifetimes(int expireAfterMinutes)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonCloudShareLinkRequest.ForFile(TargetId, expireAfterMinutes));
        }

        [Fact]
        public void Request_rejects_missing_target_id()
        {
            Assert.Throws<ArgumentException>(
                () => CottonCloudShareLinkRequest.ForFile(Guid.Empty));
        }

        [Theory]
        [InlineData("/api/v1/files/11111111-2222-3333-4444-555555555555/download?token=file-token", "file-token")]
        [InlineData("https://cloud.example/api/v1/files/11111111-2222-3333-4444-555555555555/download?token=file-token&view=inline", "file-token")]
        [InlineData("/s/folder-token", "folder-token")]
        [InlineData("https://cloud.example/s/folder-token?view=inline", "folder-token")]
        [InlineData("https://cloud.example/api/v1/files/download/file-token", "file-token")]
        public void Url_builder_extracts_backend_tokens(string backendLink, string expectedToken)
        {
            Assert.Equal(expectedToken, CottonCloudShareLinkUrlBuilder.ExtractToken(backendLink));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("/")]
        [InlineData("?token=")]
        public void Url_builder_returns_null_when_token_is_missing(string backendLink)
        {
            Assert.Null(CottonCloudShareLinkUrlBuilder.ExtractToken(backendLink));
        }

        [Fact]
        public void Url_builder_builds_public_share_url_from_file_download_link()
        {
            string url = CottonCloudShareLinkUrlBuilder.BuildShareUrl(
                InstanceUri,
                "/api/v1/files/11111111-2222-3333-4444-555555555555/download?token=file-token");

            Assert.Equal("https://cloud.example/s/file-token", url);
        }

        [Fact]
        public void Url_builder_preserves_instance_base_path_for_self_hosted_subpaths()
        {
            string url = CottonCloudShareLinkUrlBuilder.BuildShareUrl(
                new Uri("https://cloud.example/cotton"),
                "/s/folder-token");

            Assert.Equal("https://cloud.example/cotton/s/folder-token", url);
        }

        [Fact]
        public void Url_builder_escapes_custom_tokens_in_public_share_path()
        {
            Uri uri = CottonCloudShareLinkUrlBuilder.CreateShareUri(InstanceUri, "custom token");

            Assert.Equal("https://cloud.example/s/custom%20token", uri.AbsoluteUri);
        }

        [Fact]
        public void Snapshot_carries_request_and_share_url()
        {
            CottonCloudShareLinkRequest request = CottonCloudShareLinkRequest.ForFolder(
                TargetId,
                expireAfterMinutes: 60);

            CottonCloudShareLinkSnapshot snapshot = CottonCloudShareLinkSnapshot.Create(
                request,
                InstanceUri,
                "/s/folder-token");

            Assert.Equal(CottonCloudShareLinkTargetKind.Folder, snapshot.TargetKind);
            Assert.Equal(TargetId, snapshot.TargetId);
            Assert.Equal(60, snapshot.ExpireAfterMinutes);
            Assert.Equal("/s/folder-token", snapshot.BackendLink);
            Assert.Equal("folder-token", snapshot.Token);
            Assert.Equal(new Uri("https://cloud.example/s/folder-token"), snapshot.ShareUri);
            Assert.Equal("https://cloud.example/s/folder-token", snapshot.ShareUrl);
        }

        [Fact]
        public void Snapshot_requires_backend_token()
        {
            CottonCloudShareLinkRequest request = CottonCloudShareLinkRequest.ForFile(TargetId);

            Assert.Throws<ArgumentException>(
                () => CottonCloudShareLinkSnapshot.Create(request, InstanceUri, "/"));
        }
    }
}
