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
        public void Bulk_status_texts_use_item_count()
        {
            Assert.Equal("Creating 3 links...", CottonCloudShareLinkStatusText.CreateCreatingStatus(3));
            Assert.Equal("Creating link 2 of 3...", CottonCloudShareLinkStatusText.CreateCreatingItemStatus(2, 3));
            Assert.Equal("Copying 3 links...", CottonCloudShareLinkStatusText.CreateCopyingStatus(3));
            Assert.Equal("Sharing 3 links...", CottonCloudShareLinkStatusText.CreateSharingStatus(3));
            Assert.Equal("3 links copied.", CottonCloudShareLinkStatusText.CreateCopiedStatus(3));
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
        public void Bulk_creation_failure_uses_selection_copy()
        {
            string missingStatus = CottonCloudShareLinkStatusText.CreateBulkCreationFailedStatus(
                HttpStatusCode.NotFound,
                hasInternetAccess: true);
            string rejectedStatus = CottonCloudShareLinkStatusText.CreateBulkCreationFailedStatus(
                HttpStatusCode.Conflict,
                hasInternetAccess: true);

            Assert.Equal("Could not create links. Some selected items are no longer available.", missingStatus);
            Assert.Equal("Could not create links. Server rejected one of the selected items.", rejectedStatus);
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
        public void Reset_file_links_statuses_use_precise_destructive_action_copy()
        {
            Assert.Equal(
                "Offline. Reset file links needs internet.",
                CottonCloudShareLinkStatusText.ResetFileLinksOfflineUnavailableStatus);
            Assert.Equal(
                "Sign in to reset file links.",
                CottonCloudShareLinkStatusText.ResetFileLinksUnavailableStatus);
            Assert.Equal(
                "Resetting file links...",
                CottonCloudShareLinkStatusText.ResetFileLinksInProgressStatus);
            Assert.Equal(
                "File links reset.",
                CottonCloudShareLinkStatusText.ResetFileLinksCompletedStatus);
            Assert.Equal(
                "Reset file links cancelled.",
                CottonCloudShareLinkStatusText.ResetFileLinksCancelledStatus);
        }

        [Fact]
        public void Reset_file_links_failure_uses_offline_copy_when_network_is_unavailable()
        {
            string status = CottonCloudShareLinkStatusText.CreateResetFileLinksFailedStatus(
                statusCode: null,
                hasInternetAccess: false);

            Assert.Equal(CottonCloudShareLinkStatusText.ResetFileLinksOfflineUnavailableStatus, status);
        }

        [Fact]
        public void Reset_file_links_failure_uses_sign_in_copy_for_forbidden_response()
        {
            string status = CottonCloudShareLinkStatusText.CreateResetFileLinksFailedStatus(
                HttpStatusCode.Forbidden,
                hasInternetAccess: true);

            Assert.Equal("Could not reset file links. Sign in again.", status);
        }

        [Fact]
        public void Reset_file_links_failure_uses_generic_copy_for_unknown_response()
        {
            string status = CottonCloudShareLinkStatusText.CreateResetFileLinksFailedStatus(
                HttpStatusCode.InternalServerError,
                hasInternetAccess: true);

            Assert.Equal("Could not reset file links.", status);
        }
    }
}
