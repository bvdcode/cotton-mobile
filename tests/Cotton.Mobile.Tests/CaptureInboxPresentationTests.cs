using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CaptureInboxPresentationTests
    {
        private static readonly DateTime OldReceivedAt = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime NewReceivedAt = new(2026, 6, 19, 11, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Snapshot_reports_empty_capture_inbox_state()
        {
            CottonCaptureInboxListSnapshot snapshot = CottonCaptureInboxListSnapshot.Create([]);

            Assert.True(snapshot.IsEmpty);
            Assert.Empty(snapshot.Items);
            Assert.Equal("0 captured items", snapshot.SummaryText);
            Assert.Equal("No captured items", snapshot.EmptyMessage);
            Assert.False(string.IsNullOrWhiteSpace(snapshot.EmptyDetails));
        }

        [Fact]
        public void Snapshot_sorts_newest_intake_first_and_flattens_items()
        {
            CottonShareIntakeSnapshot oldSnapshot = CreatePending(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "old.txt",
                OldReceivedAt);
            CottonShareIntakeSnapshot newSnapshot = CreatePending(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "new.txt",
                NewReceivedAt);

            CottonCaptureInboxListSnapshot snapshot =
                CottonCaptureInboxListSnapshot.Create([oldSnapshot, newSnapshot]);

            Assert.Equal("new.txt", snapshot.Items[0].DisplayName);
            Assert.Equal("old.txt", snapshot.Items[1].DisplayName);
            Assert.Equal("2 captured items", snapshot.SummaryText);
        }

        [Fact]
        public void Snapshot_formats_staged_file_text_and_missing_permission_states()
        {
            CottonShareIntakeSnapshot stagedFile = CreatePending(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "photo.jpg",
                NewReceivedAt);
            CottonShareIntakeSnapshot textShare = CottonShareIntakeSnapshot.CreatePending(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                CottonShareIntakeKind.Send,
                "text/plain",
                [
                    new CottonShareIntakeItemSnapshot(
                        Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        CottonShareIntakeItemType.Text,
                        "hello from another app",
                        displayName: null,
                        mimeType: "text/plain")
                ],
                NewReceivedAt.AddMinutes(1));
            CottonShareIntakeSnapshot missingAccess = CottonShareIntakeSnapshot.CreateProblem(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                CottonShareIntakeKind.Send,
                CottonShareIntakeStatus.MissingPermission,
                "application/pdf",
                [
                    new CottonShareIntakeItemSnapshot(
                        Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        CottonShareIntakeItemType.Uri,
                        "content://docs/report",
                        "report.pdf",
                        "application/pdf")
                ],
                "Android revoked access to the shared content.",
                NewReceivedAt.AddMinutes(2));

            CottonCaptureInboxListSnapshot snapshot =
                CottonCaptureInboxListSnapshot.Create([stagedFile, textShare, missingAccess]);

            CottonCaptureInboxListItem missingItem = Find(snapshot, "report.pdf");
            Assert.Equal("Needs access", missingItem.StatusText);
            Assert.True(missingItem.IsFailureVisible);
            Assert.Equal("Android revoked access to the shared content.", missingItem.FailureMessage);

            CottonCaptureInboxListItem textItem = Find(snapshot, "hello from another app");
            Assert.Equal("Text", textItem.KindText);
            Assert.Equal("Ready", textItem.StatusText);
            Assert.Equal("Text share captured", textItem.DetailText);
            Assert.False(textItem.IsDestinationVisible);
            Assert.False(textItem.CanSelectDestination);
            Assert.False(textItem.IsFailureVisible);

            CottonCaptureInboxListItem fileItem = Find(snapshot, "photo.jpg");
            Assert.Equal("File", fileItem.KindText);
            Assert.Equal("Choose folder", fileItem.StatusText);
            Assert.Equal("Copied to this device", fileItem.DetailText);
            Assert.Equal("No destination selected", fileItem.DestinationText);
            Assert.True(fileItem.IsDestinationVisible);
            Assert.True(fileItem.CanSelectDestination);
            Assert.Contains("12 B", fileItem.MetadataText, StringComparison.Ordinal);
            Assert.False(fileItem.IsFailureVisible);
        }

        [Fact]
        public void Snapshot_formats_selected_destination_for_staged_files()
        {
            CottonShareIntakeSnapshot stagedFile = CreatePending(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "photo.jpg",
                    NewReceivedAt)
                .WithDestination(
                    new CottonShareDestinationSnapshot(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        "Camera Uploads",
                        "Files / Camera Uploads"));

            CottonCaptureInboxListItem fileItem =
                Assert.Single(CottonCaptureInboxListSnapshot.Create([stagedFile]).Items);

            Assert.Equal("Ready", fileItem.StatusText);
            Assert.Equal("Destination: Files / Camera Uploads", fileItem.DestinationText);
            Assert.True(fileItem.IsDestinationVisible);
            Assert.True(fileItem.CanSelectDestination);
        }

        private static CottonShareIntakeSnapshot CreatePending(Guid id, string displayName, DateTime receivedAtUtc)
        {
            Guid itemId = Guid.NewGuid();
            var staged = new CottonShareStagedContentSnapshot(
                id,
                itemId,
                displayName,
                $"/tmp/{displayName}",
                12);
            var item = new CottonShareIntakeItemSnapshot(
                    itemId,
                    CottonShareIntakeItemType.Uri,
                    $"content://shared/{displayName}",
                    displayName,
                    "text/plain")
                .WithStagedContent(staged);
            return CottonShareIntakeSnapshot.CreatePending(
                id,
                CottonShareIntakeKind.Send,
                "text/plain",
                [item],
                receivedAtUtc);
        }

        private static CottonCaptureInboxListItem Find(
            CottonCaptureInboxListSnapshot snapshot,
            string displayName)
        {
            return snapshot.Items.Single(item => item.DisplayName == displayName);
        }
    }
}
