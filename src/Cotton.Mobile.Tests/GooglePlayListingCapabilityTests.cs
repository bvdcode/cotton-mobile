using System.Text.Json;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class GooglePlayListingCapabilityTests
    {
        private const string ListingDirectory = "store/google-play/default-listing";

        [Fact]
        public void ListingCapabilityContractMatchesTheProductModel()
        {
            using JsonDocument document = JsonDocument.Parse(
                RepositoryPath.ReadText($"{ListingDirectory}/capabilities.json"));
            JsonElement root = document.RootElement;

            Assert.Equal([CottonSyncDirection.DeviceToCloud], Enum.GetValues<CottonSyncDirection>());
            Assert.Equal("device-to-cloud", root.GetProperty("direction").GetString());
            Assert.True(root.GetProperty("uploadsNewFiles").GetBoolean());
            Assert.False(root.GetProperty("uploadsChangedFiles").GetBoolean());
            Assert.False(root.GetProperty("downloadsFiles").GetBoolean());
            Assert.False(root.GetProperty("twoWaySync").GetBoolean());
            Assert.True(root.GetProperty("deleteAfterConfirmedUpload").GetBoolean());
        }

        [Fact]
        public void ListingCopyDoesNotPromiseUnsupportedTransfers()
        {
            string listingText = string.Join(
                    '\n',
                    RepositoryPath.ReadText($"{ListingDirectory}/short-description.txt"),
                    RepositoryPath.ReadText($"{ListingDirectory}/full-description.txt"),
                    RepositoryPath.ReadText($"{ListingDirectory}/graphics/metadata.json"))
                .ToLowerInvariant();

            Assert.DoesNotContain("sync changes in both directions", listingText, StringComparison.Ordinal);
            Assert.DoesNotContain("upload new and changed", listingText, StringComparison.Ordinal);
            Assert.DoesNotContain("cloud to device", listingText, StringComparison.Ordinal);
            Assert.Contains("does not download", listingText, StringComparison.Ordinal);
            Assert.Contains("does not", listingText, StringComparison.Ordinal);
            Assert.Contains("two-way synchronization", listingText, StringComparison.Ordinal);
        }
    }
}
