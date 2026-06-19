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
            Assert.False(fileItem.CanEnqueue);
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
            Assert.True(fileItem.CanEnqueue);
        }

        [Fact]
        public void Snapshot_classifies_staged_image_shares_from_mime_type()
        {
            CottonShareIntakeSnapshot imageShare = CottonShareIntakeSnapshot
                .CreatePending(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    CottonShareIntakeKind.Send,
                    "image/jpeg",
                    [
                        CreateStagedItem(
                            Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            "IMG_2042.JPG",
                            68,
                            "image/jpeg"),
                    ],
                    NewReceivedAt)
                .WithDestination(
                    new CottonShareDestinationSnapshot(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        "Default",
                        "Default"));

            CottonCaptureInboxListItem imageItem =
                Assert.Single(CottonCaptureInboxListSnapshot.Create([imageShare]).Items);

            Assert.Equal("IMG_2042.JPG", imageItem.DisplayName);
            Assert.Equal("Image", imageItem.KindText);
            Assert.Equal("Ready", imageItem.StatusText);
            Assert.Equal("Copied to this device", imageItem.DetailText);
            Assert.Equal("Destination: Default", imageItem.DestinationText);
            Assert.Contains("68 B", imageItem.MetadataText, StringComparison.Ordinal);
            Assert.Contains("image/jpeg", imageItem.MetadataText, StringComparison.Ordinal);
            Assert.True(imageItem.CanSelectDestination);
            Assert.True(imageItem.CanRename);
            Assert.True(imageItem.CanEnqueue);
        }

        [Theory]
        [InlineData("report.pdf", "application/pdf", "PDF", 389)]
        [InlineData("brief.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Document", 512)]
        public void Snapshot_classifies_staged_document_shares_from_mime_type(
            string displayName,
            string mimeType,
            string expectedKind,
            long sizeBytes)
        {
            CottonShareIntakeSnapshot documentShare = CottonShareIntakeSnapshot
                .CreatePending(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    CottonShareIntakeKind.Send,
                    mimeType,
                    [
                        CreateStagedItem(
                            Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            displayName,
                            sizeBytes,
                            mimeType),
                    ],
                    NewReceivedAt)
                .WithDestination(
                    new CottonShareDestinationSnapshot(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        "Default",
                        "Default"));

            CottonCaptureInboxListItem documentItem =
                Assert.Single(CottonCaptureInboxListSnapshot.Create([documentShare]).Items);

            Assert.Equal(displayName, documentItem.DisplayName);
            Assert.Equal(expectedKind, documentItem.KindText);
            Assert.Equal("Ready", documentItem.StatusText);
            Assert.Equal("Copied to this device", documentItem.DetailText);
            Assert.Equal("Destination: Default", documentItem.DestinationText);
            Assert.Contains(CottonFileSizeFormatter.Format(sizeBytes), documentItem.MetadataText, StringComparison.Ordinal);
            Assert.Contains(mimeType, documentItem.MetadataText, StringComparison.Ordinal);
            Assert.True(documentItem.CanSelectDestination);
            Assert.True(documentItem.CanRename);
            Assert.True(documentItem.CanEnqueue);
        }

        [Fact]
        public void Snapshot_flattens_multiple_staged_files_with_shared_destination()
        {
            Guid intakeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            CottonShareIntakeSnapshot stagedFiles = CottonShareIntakeSnapshot
                .CreatePending(
                    intakeId,
                    CottonShareIntakeKind.SendMultiple,
                    "text/plain",
                    [
                        CreateStagedItem(intakeId, "first.txt", 12),
                        CreateStagedItem(intakeId, "second.txt", 14),
                    ],
                    NewReceivedAt)
                .WithDestination(
                    new CottonShareDestinationSnapshot(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        "Default",
                        "Default"));

            CottonCaptureInboxListSnapshot snapshot =
                CottonCaptureInboxListSnapshot.Create([stagedFiles]);

            Assert.Equal("2 captured items", snapshot.SummaryText);
            Assert.Equal(["first.txt", "second.txt"], snapshot.Items.Select(item => item.DisplayName).ToArray());
            Assert.All(snapshot.Items, item =>
            {
                Assert.Equal("Ready", item.StatusText);
                Assert.Equal("Copied to this device", item.DetailText);
                Assert.Equal("Destination: Default", item.DestinationText);
                Assert.True(item.CanSelectDestination);
                Assert.True(item.CanRename);
                Assert.True(item.CanEnqueue);
            });
        }

        [Fact]
        public void Snapshot_uses_upload_display_name_for_renamed_staged_files()
        {
            CottonShareIntakeSnapshot stagedFile = CreatePending(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "photo.jpg",
                NewReceivedAt);
            CottonShareIntakeItemSnapshot renamedItem = Assert
                .Single(stagedFile.Items)
                .WithUploadDisplayName("Trip to Paris.jpg");
            stagedFile = new CottonShareIntakeSnapshot(
                stagedFile.Id,
                stagedFile.Kind,
                stagedFile.Status,
                stagedFile.SourceMimeType,
                [renamedItem],
                stagedFile.FailureMessage,
                stagedFile.ReceivedAtUtc,
                stagedFile.Destination);

            CottonCaptureInboxListItem fileItem =
                Assert.Single(CottonCaptureInboxListSnapshot.Create([stagedFile]).Items);

            Assert.Equal("Trip to Paris.jpg", fileItem.DisplayName);
            Assert.True(fileItem.CanRename);
        }

        private static CottonShareIntakeSnapshot CreatePending(Guid id, string displayName, DateTime receivedAtUtc)
        {
            return CottonShareIntakeSnapshot.CreatePending(
                id,
                CottonShareIntakeKind.Send,
                "text/plain",
                [CreateStagedItem(id, displayName, 12)],
                receivedAtUtc);
        }

        private static CottonShareIntakeItemSnapshot CreateStagedItem(
            Guid intakeId,
            string displayName,
            long sizeBytes,
            string mimeType = "application/octet-stream")
        {
            Guid itemId = Guid.NewGuid();
            var staged = new CottonShareStagedContentSnapshot(
                intakeId,
                itemId,
                displayName,
                $"/tmp/{displayName}",
                sizeBytes);
            return new CottonShareIntakeItemSnapshot(
                    itemId,
                    CottonShareIntakeItemType.Uri,
                    $"content://shared/{displayName}",
                    displayName,
                    mimeType)
                .WithStagedContent(staged);
        }

        private static CottonCaptureInboxListItem Find(
            CottonCaptureInboxListSnapshot snapshot,
            string displayName)
        {
            return snapshot.Items.Single(item => item.DisplayName == displayName);
        }
    }
}
