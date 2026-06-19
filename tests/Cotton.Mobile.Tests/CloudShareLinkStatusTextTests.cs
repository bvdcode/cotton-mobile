using System.Net;
using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CloudShareLinkStatusTextTests
    {
        [Fact]
        public void Creates_action_progress_and_success_text()
        {
            Assert.Equal(
                "Creating link for Roadmap...",
                CottonCloudShareLinkStatusText.CreateCreatingStatus(" Roadmap "));
            Assert.Equal(
                "Copying link for Roadmap...",
                CottonCloudShareLinkStatusText.CreateCopyingStatus("Roadmap"));
            Assert.Equal(
                "Sharing link for Roadmap...",
                CottonCloudShareLinkStatusText.CreateSharingStatus("Roadmap"));
            Assert.Equal(
                "Link copied for Roadmap.",
                CottonCloudShareLinkStatusText.CreateCopiedStatus("Roadmap"));
        }

        [Fact]
        public void Uses_item_fallback_for_blank_target_name()
        {
            Assert.Equal(
                "Creating link for item...",
                CottonCloudShareLinkStatusText.CreateCreatingStatus(" "));
        }

        [Fact]
        public void Offline_creation_failure_uses_retryable_offline_copy()
        {
            string status = CottonCloudShareLinkStatusText.CreateCreationFailedStatus(
                CottonCloudShareLinkTargetKind.File,
                statusCode: null,
                hasInternetAccess: false);

            Assert.Equal(CottonCloudShareLinkStatusText.OfflineUnavailableStatus, status);
        }

        [Theory]
        [InlineData(CottonCloudShareLinkTargetKind.File, "Could not create link. File is no longer available.")]
        [InlineData(CottonCloudShareLinkTargetKind.Folder, "Could not create link. Folder is no longer available.")]
        public void Missing_targets_get_specific_creation_failure(
            CottonCloudShareLinkTargetKind targetKind,
            string expected)
        {
            string status = CottonCloudShareLinkStatusText.CreateCreationFailedStatus(
                targetKind,
                HttpStatusCode.NotFound,
                hasInternetAccess: true);

            Assert.Equal(expected, status);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.UnprocessableEntity)]
        public void Rejected_targets_get_server_rejection_copy(HttpStatusCode statusCode)
        {
            string status = CottonCloudShareLinkStatusText.CreateCreationFailedStatus(
                CottonCloudShareLinkTargetKind.Folder,
                statusCode,
                hasInternetAccess: true);

            Assert.Equal("Could not create link. Server rejected this folder.", status);
        }

        [Fact]
        public void Unknown_creation_failure_uses_generic_copy()
        {
            string status = CottonCloudShareLinkStatusText.CreateCreationFailedStatus(
                CottonCloudShareLinkTargetKind.File,
                HttpStatusCode.InternalServerError,
                hasInternetAccess: true);

            Assert.Equal("Could not create link.", status);
        }

        [Fact]
        public void Reset_all_links_statuses_use_explicit_destructive_action_copy()
        {
            Assert.Equal(
                "Offline. Reset shared links needs internet.",
                CottonCloudShareLinkStatusText.ResetAllOfflineUnavailableStatus);
            Assert.Equal(
                "Sign in to reset shared links.",
                CottonCloudShareLinkStatusText.ResetAllUnavailableStatus);
            Assert.Equal(
                "Resetting shared links...",
                CottonCloudShareLinkStatusText.ResetAllInProgressStatus);
            Assert.Equal(
                "Shared links reset.",
                CottonCloudShareLinkStatusText.ResetAllCompletedStatus);
            Assert.Equal(
                "Reset shared links cancelled.",
                CottonCloudShareLinkStatusText.ResetAllCancelledStatus);
        }

        [Fact]
        public void Reset_all_failure_uses_offline_copy_when_network_is_unavailable()
        {
            string status = CottonCloudShareLinkStatusText.CreateResetAllFailedStatus(
                statusCode: null,
                hasInternetAccess: false);

            Assert.Equal(CottonCloudShareLinkStatusText.ResetAllOfflineUnavailableStatus, status);
        }

        [Fact]
        public void Reset_all_failure_uses_sign_in_copy_for_forbidden_response()
        {
            string status = CottonCloudShareLinkStatusText.CreateResetAllFailedStatus(
                HttpStatusCode.Forbidden,
                hasInternetAccess: true);

            Assert.Equal("Could not reset shared links. Sign in again.", status);
        }

        [Fact]
        public void Reset_all_failure_uses_generic_copy_for_unknown_response()
        {
            string status = CottonCloudShareLinkStatusText.CreateResetAllFailedStatus(
                HttpStatusCode.InternalServerError,
                hasInternetAccess: true);

            Assert.Equal("Could not reset shared links.", status);
        }
    }
}
