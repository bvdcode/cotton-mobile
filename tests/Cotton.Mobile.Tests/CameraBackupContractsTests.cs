using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CameraBackupContractsTests
    {
        private static readonly Guid DestinationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        [Fact]
        public void Default_camera_backup_settings_are_safe_and_disabled()
        {
            CottonCameraBackupSettings settings = CottonCameraBackupSettings.Default;

            Assert.False(settings.IsEnabled);
            Assert.Null(settings.Destination);
            Assert.True(settings.PhotosOnly);
            Assert.True(settings.WifiOnly);
            Assert.False(settings.AllowCellular);
            Assert.False(settings.ChargingOnly);
            Assert.False(settings.CanRunBackup);
        }

        [Fact]
        public void Camera_backup_destination_is_normalized_for_display()
        {
            var destination = new CottonUploadDestinationSnapshot(
                DestinationId,
                " Camera ",
                " Files / Camera ");

            CottonCameraBackupSettings settings = CottonCameraBackupSettings.Default.WithDestination(destination);
            CottonCameraBackupSetupDisplayState display = CottonCameraBackupSetupDisplayState.Create(settings);

            Assert.Equal(DestinationId, settings.Destination?.FolderId);
            Assert.Equal("Camera", settings.Destination?.FolderName);
            Assert.Equal("Files / Camera", settings.Destination?.Path);
            Assert.Equal("Files / Camera", display.DestinationText);
            Assert.True(display.IsDestinationSelected);
        }

        [Fact]
        public void Camera_backup_network_policy_keeps_wifi_and_cellular_mutually_exclusive()
        {
            CottonCameraBackupSettings cellular =
                CottonCameraBackupSettings.Default.WithAllowCellular(true);
            CottonCameraBackupSettings wifi =
                cellular.WithWifiOnly(true);

            Assert.True(cellular.AllowCellular);
            Assert.False(cellular.WifiOnly);

            Assert.True(wifi.WifiOnly);
            Assert.False(wifi.AllowCellular);
        }

        [Fact]
        public void Camera_backup_setup_display_reports_honest_execution_state()
        {
            CottonCameraBackupSetupDisplayState missingDestination =
                CottonCameraBackupSetupDisplayState.Create(CottonCameraBackupSettings.Default);
            CottonCameraBackupSetupDisplayState withDestination =
                CottonCameraBackupSetupDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()));
            CottonCameraBackupSetupDisplayState enabled =
                CottonCameraBackupSetupDisplayState.Create(
                    CottonCameraBackupSettings.Default
                        .WithDestination(CreateDestination())
                        .WithEnabled(true));

            Assert.Equal("No folder selected", missingDestination.DestinationText);
            Assert.Equal(string.Empty, missingDestination.ExecutionStatusText);
            Assert.False(missingDestination.CanEnableBackup);

            Assert.Equal("Camera backup is off.", withDestination.ExecutionStatusText);
            Assert.True(withDestination.CanEnableBackup);
            Assert.Equal("Camera backup is on.", enabled.ExecutionStatusText);
        }

        [Fact]
        public void Camera_backup_mvp_keeps_local_media_on_device()
        {
            CottonCameraBackupLocalMediaRetentionPolicy policy =
                CottonCameraBackupLocalMediaRetentionPolicy.Mvp;
            CottonCameraBackupSetupDisplayState display =
                CottonCameraBackupSetupDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()));

            Assert.False(policy.DeletesSourceMedia);
            Assert.False(policy.RequiresMediaDeletePermission);
            Assert.Equal("Queue camera media to Cotton. Originals stay on this device.", policy.SetupSummaryText);
            Assert.Equal(policy.SetupSummaryText, display.LocalMediaRetentionText);
        }

        [Theory]
        [InlineData(CottonCameraBackupMediaAccessState.NotRequested, "Not requested", "Allow", false, false, false, false)]
        [InlineData(CottonCameraBackupMediaAccessState.Allowed, "Allowed", "", true, true, false, false)]
        [InlineData(CottonCameraBackupMediaAccessState.Limited, "Selected media only", "Settings", true, false, true, true)]
        [InlineData(CottonCameraBackupMediaAccessState.Denied, "Denied", "Settings", false, false, true, true)]
        [InlineData(CottonCameraBackupMediaAccessState.Unavailable, "Unavailable", "", false, false, true, false)]
        public void Camera_backup_media_access_display_keeps_permission_state_explicit(
            CottonCameraBackupMediaAccessState state,
            string statusText,
            string actionText,
            bool canReadMedia,
            bool canScanFullLibrary,
            bool needsAttention,
            bool shouldOpenSettings)
        {
            CottonCameraBackupMediaAccessDisplayState display =
                CottonCameraBackupMediaAccessDisplayState.Create(state);

            Assert.Equal("Media access", display.Title);
            Assert.Equal(statusText, display.StatusText);
            Assert.Equal(actionText, display.ActionText);
            Assert.Equal(!string.IsNullOrWhiteSpace(actionText), display.IsActionVisible);
            Assert.Equal(canReadMedia, display.CanReadMedia);
            Assert.Equal(canScanFullLibrary, display.CanScanFullLibrary);
            Assert.Equal(needsAttention, display.NeedsAttention);
            Assert.Equal(shouldOpenSettings, display.ShouldOpenSettings);
            Assert.False(string.IsNullOrWhiteSpace(display.DetailText));
        }

        [Theory]
        [InlineData(CottonCameraBackupMediaAccessState.NotRequested, false, false)]
        [InlineData(CottonCameraBackupMediaAccessState.Allowed, true, true)]
        [InlineData(CottonCameraBackupMediaAccessState.Limited, true, false)]
        [InlineData(CottonCameraBackupMediaAccessState.Denied, false, false)]
        [InlineData(CottonCameraBackupMediaAccessState.Unavailable, false, false)]
        public void Camera_backup_media_access_rules_keep_partial_access_from_full_backup(
            CottonCameraBackupMediaAccessState state,
            bool canReadAnyMedia,
            bool canScanFullLibrary)
        {
            Assert.Equal(canReadAnyMedia, CottonCameraBackupMediaAccessRules.CanReadAnyMedia(state));
            Assert.Equal(canScanFullLibrary, CottonCameraBackupMediaAccessRules.CanScanFullLibrary(state));
        }

        [Fact]
        public void Camera_backup_rejects_enabled_state_without_destination()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new CottonCameraBackupSettings(
                    isEnabled: true,
                    destination: null,
                    photosOnly: true,
                    wifiOnly: true,
                    allowCellular: false,
                    chargingOnly: false));

            var settings = new CottonCameraBackupSettings(
                    isEnabled: true,
                    CreateDestination(),
                    photosOnly: true,
                    wifiOnly: true,
                    allowCellular: false,
                    chargingOnly: false);

            Assert.True(settings.IsEnabled);
        }

        [Fact]
        public void Camera_backup_enabled_state_updates_after_destination_is_available()
        {
            CottonCameraBackupSettings settings =
                CottonCameraBackupSettings.Default.WithDestination(CreateDestination());

            CottonCameraBackupSettings enabled = settings.WithEnabled(true);
            CottonCameraBackupSettings disabled = enabled.WithEnabled(false);

            Assert.True(enabled.IsEnabled);
            Assert.False(disabled.IsEnabled);
            Assert.Equal(settings.Destination, enabled.Destination);
            Assert.Equal(settings.PhotosOnly, enabled.PhotosOnly);
            Assert.Equal(settings.WifiOnly, enabled.WifiOnly);
            Assert.Equal(settings.AllowCellular, enabled.AllowCellular);
            Assert.Equal(settings.ChargingOnly, enabled.ChargingOnly);
        }

        [Fact]
        public void Camera_backup_view_model_does_not_expose_placeholder_enable_toggle()
        {
            string source = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/BackupSetupViewModel.cs");

            Assert.DoesNotContain("public bool IsBackupEnabled => false;", source, StringComparison.Ordinal);
            Assert.DoesNotContain("public bool CanEnableBackup => false;", source, StringComparison.Ordinal);
            Assert.Contains(".WithEnabled(IsBackupEnabled && CanEnableBackup)", source, StringComparison.Ordinal);
        }

        [Fact]
        public void Camera_backup_media_identity_is_stable_until_source_version_changes()
        {
            var identity = new CottonCameraBackupMediaIdentity(
                " media://photo/1 ",
                new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc),
                2048);
            var sameIdentity = new CottonCameraBackupMediaIdentity(
                "media://photo/1",
                new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Unspecified),
                2048);
            var changedIdentity = new CottonCameraBackupMediaIdentity(
                "media://photo/1",
                new DateTime(2026, 6, 19, 12, 1, 0, DateTimeKind.Utc),
                2048);

            Assert.Equal(identity, sameIdentity);
            Assert.NotEqual(identity, changedIdentity);
            Assert.Equal(DateTimeKind.Utc, identity.LastModifiedUtc?.Kind);
            Assert.Throws<ArgumentException>(() => new CottonCameraBackupMediaIdentity(" ", null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CottonCameraBackupMediaIdentity("media://photo/2", null, -1));
        }

        [Fact]
        public void Camera_backup_candidate_normalizes_display_and_content_type()
        {
            var photo = new CottonCameraBackupCandidate(
                new CottonCameraBackupMediaIdentity("media://photo/1", null, 100),
                CottonCameraBackupMediaKind.Photo,
                " /storage/emulated/0/DCIM/photo.jpg ",
                " image/jpeg ",
                new DateTime(2026, 6, 19, 10, 0, 0, DateTimeKind.Unspecified));
            var video = new CottonCameraBackupCandidate(
                new CottonCameraBackupMediaIdentity("media://video/1", null, 200),
                CottonCameraBackupMediaKind.Video,
                " ",
                null,
                null);

            Assert.Equal("photo.jpg", photo.DisplayName);
            Assert.Equal("image/jpeg", photo.ContentType);
            Assert.Equal(DateTimeKind.Utc, photo.CapturedAtUtc?.Kind);
            Assert.Equal("video.mp4", video.DisplayName);
            Assert.Equal("video/mp4", video.ContentType);
        }

        [Fact]
        public void Camera_backup_media_source_record_maps_image_media()
        {
            var record = new CottonCameraBackupMediaSourceRecord(
                " content://media/external/images/media/12 ",
                " /storage/emulated/0/DCIM/Camera/IMG_0001.JPG ",
                " image/jpeg ",
                2048,
                new DateTime(2026, 6, 19, 11, 0, 0, DateTimeKind.Unspecified),
                new DateTime(2026, 6, 19, 10, 30, 0, DateTimeKind.Local));

            bool mapped = CottonCameraBackupMediaSourceRecordMapper.TryCreateCandidate(record, out CottonCameraBackupCandidate? candidate);

            Assert.True(mapped);
            Assert.NotNull(candidate);
            Assert.Equal(CottonCameraBackupMediaKind.Photo, candidate.Kind);
            Assert.Equal("content://media/external/images/media/12", candidate.Identity.SourceId);
            Assert.Equal("IMG_0001.JPG", candidate.DisplayName);
            Assert.Equal("image/jpeg", candidate.ContentType);
            Assert.Equal(DateTimeKind.Utc, candidate.Identity.LastModifiedUtc?.Kind);
            Assert.Equal(DateTimeKind.Utc, candidate.CapturedAtUtc?.Kind);
        }

        [Fact]
        public void Camera_backup_media_source_record_maps_video_media_case_insensitively()
        {
            var record = new CottonCameraBackupMediaSourceRecord(
                "content://media/external/video/media/7",
                "VID_0007.MP4",
                " Video/MP4 ",
                4096,
                null,
                null);

            bool mapped = CottonCameraBackupMediaSourceRecordMapper.TryCreateCandidate(record, out CottonCameraBackupCandidate? candidate);

            Assert.True(mapped);
            Assert.NotNull(candidate);
            Assert.Equal(CottonCameraBackupMediaKind.Video, candidate.Kind);
            Assert.Equal("VID_0007.MP4", candidate.DisplayName);
            Assert.Equal("Video/MP4", candidate.ContentType);
            Assert.True(candidate.IsVideo);
        }

        [Theory]
        [InlineData("image/heic", CottonCameraBackupMediaKind.Photo, "photo.jpg")]
        [InlineData("video/quicktime", CottonCameraBackupMediaKind.Video, "video.mp4")]
        public void Camera_backup_media_source_record_uses_stable_defaults_for_blank_names(
            string contentType,
            CottonCameraBackupMediaKind expectedKind,
            string expectedDisplayName)
        {
            var record = new CottonCameraBackupMediaSourceRecord(
                "content://media/external/media/99",
                " ",
                contentType,
                0,
                new DateTime(2026, 6, 19, 13, 0, 0, DateTimeKind.Utc),
                null);

            bool mapped = CottonCameraBackupMediaSourceRecordMapper.TryCreateCandidate(
                record,
                out CottonCameraBackupCandidate? candidate);

            Assert.True(mapped);
            Assert.NotNull(candidate);
            Assert.Equal(expectedKind, candidate.Kind);
            Assert.Equal(expectedDisplayName, candidate.DisplayName);
            Assert.Equal(contentType, candidate.ContentType);
            Assert.Equal(0, candidate.Identity.SizeBytes);
        }

        [Theory]
        [InlineData(null, "content://media/external/file/1", 1024)]
        [InlineData(" ", "content://media/external/file/1", 1024)]
        [InlineData("application/pdf", "content://media/external/file/1", 1024)]
        [InlineData("image/jpeg", " ", 1024)]
        [InlineData("video/mp4", "content://media/external/video/media/7", -1)]
        public void Camera_backup_media_source_record_rejects_unsupported_or_unstable_media(
            string? contentType,
            string? sourceId,
            int? sizeBytes)
        {
            var record = new CottonCameraBackupMediaSourceRecord(
                sourceId,
                "media.bin",
                contentType,
                sizeBytes,
                null,
                null);

            bool mapped = CottonCameraBackupMediaSourceRecordMapper.TryCreateCandidate(record, out CottonCameraBackupCandidate? candidate);

            Assert.False(mapped);
            Assert.Null(candidate);
        }

        [Fact]
        public async Task Camera_backup_scanner_filters_photos_only_and_suppresses_known_media()
        {
            CottonCameraBackupCandidate photo = CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo);
            CottonCameraBackupCandidate duplicatePhoto = CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo);
            CottonCameraBackupCandidate video = CreateCandidate("media://video/1", CottonCameraBackupMediaKind.Video);
            CottonCameraBackupCandidate uploadedPhoto = CreateCandidate("media://photo/2", CottonCameraBackupMediaKind.Photo);
            var scanner = new CottonCameraBackupScanner(
                new StubCameraBackupMediaSource(photo, duplicatePhoto, video, uploadedPhoto));
            var uploaded = new[]
            {
                new CottonCameraBackupUploadedMediaSnapshot(
                    uploadedPhoto.Identity,
                    new DateTime(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc),
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "photo-2.jpg"),
            };

            CottonCameraBackupScanResult result =
                await scanner.ScanAsync(CottonCameraBackupSettings.Default, uploaded);

            Assert.Equal(4, result.ScannedCount);
            Assert.Equal(2, result.SkippedAlreadyTrackedCount);
            Assert.Equal(1, result.SkippedByPolicyCount);
            CottonCameraBackupCandidate candidate = Assert.Single(result.Candidates);
            Assert.Equal("media://photo/1", candidate.Identity.SourceId);
        }

        [Fact]
        public async Task Camera_backup_scanner_includes_videos_when_policy_allows_them()
        {
            var scanner = new CottonCameraBackupScanner(
                new StubCameraBackupMediaSource(
                    CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo),
                    CreateCandidate("media://video/1", CottonCameraBackupMediaKind.Video)));
            CottonCameraBackupSettings settings = CottonCameraBackupSettings.Default.WithPhotosOnly(false);

            CottonCameraBackupScanResult result =
                await scanner.ScanAsync(settings, Array.Empty<CottonCameraBackupUploadedMediaSnapshot>());

            Assert.Equal(2, result.Candidates.Count);
            Assert.Contains(result.Candidates, item => item.IsPhoto);
            Assert.Contains(result.Candidates, item => item.IsVideo);
            Assert.Equal(0, result.SkippedByPolicyCount);
        }

        [Fact]
        public void Camera_backup_destination_storage_estimate_sums_known_pending_media_size()
        {
            var scanResult = new CottonCameraBackupScanResult(
                [
                    CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo),
                    CreateCandidate("media://video/1", CottonCameraBackupMediaKind.Video),
                ],
                scannedCount: 2,
                skippedAlreadyTrackedCount: 0,
                skippedByPolicyCount: 0);

            CottonCameraBackupDestinationStorageEstimate estimate =
                CottonCameraBackupDestinationStorageEstimate.Create(scanResult);

            Assert.Equal(2, estimate.PendingCount);
            Assert.Equal(5120, estimate.KnownSizeBytes);
            Assert.Equal(0, estimate.UnknownSizeCount);
            Assert.Equal(2, estimate.KnownSizeCount);
            Assert.True(estimate.HasPendingItems);
            Assert.False(estimate.HasUnknownSizes);
        }

        [Fact]
        public void Camera_backup_destination_storage_estimate_keeps_unknown_sizes_explicit()
        {
            var scanResult = new CottonCameraBackupScanResult(
                [
                    CreateCandidate("media://photo/1", CottonCameraBackupMediaKind.Photo),
                    new CottonCameraBackupCandidate(
                        new CottonCameraBackupMediaIdentity("media://photo/2", null, null),
                        CottonCameraBackupMediaKind.Photo,
                        "photo-2.jpg",
                        "image/jpeg",
                        null),
                ],
                scannedCount: 2,
                skippedAlreadyTrackedCount: 0,
                skippedByPolicyCount: 0);

            CottonCameraBackupDestinationStorageEstimate estimate =
                CottonCameraBackupDestinationStorageEstimate.Create(scanResult);

            Assert.Equal(2, estimate.PendingCount);
            Assert.Equal(1024, estimate.KnownSizeBytes);
            Assert.Equal(1, estimate.UnknownSizeCount);
            Assert.Equal(1, estimate.KnownSizeCount);
            Assert.True(estimate.HasUnknownSizes);
        }

        [Fact]
        public void Camera_backup_destination_storage_estimate_display_is_honest_before_setup_is_complete()
        {
            CottonCameraBackupMediaAccessDisplayState noAccess =
                CottonCameraBackupMediaAccessDisplayState.Create(CottonCameraBackupMediaAccessState.NotRequested);
            CottonCameraBackupMediaAccessDisplayState allowed =
                CottonCameraBackupMediaAccessDisplayState.Create(CottonCameraBackupMediaAccessState.Allowed);

            Assert.Equal(
                string.Empty,
                CottonCameraBackupDestinationStorageEstimateDisplayState.Create(
                    CottonCameraBackupSettings.Default,
                    allowed,
                    CottonCameraBackupDestinationStorageEstimate.Empty,
                    isCurrent: true).SummaryText);
            Assert.Equal(
                "Allow full media access to estimate backup storage.",
                CottonCameraBackupDestinationStorageEstimateDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()),
                    noAccess,
                    CottonCameraBackupDestinationStorageEstimate.Empty,
                    isCurrent: true).SummaryText);
            Assert.Equal(
                "Save to refresh backup storage estimate.",
                CottonCameraBackupDestinationStorageEstimateDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()),
                    allowed,
                    CottonCameraBackupDestinationStorageEstimate.Empty,
                    isCurrent: false).SummaryText);
        }

        [Theory]
        [InlineData(2, 5120, 0, "2 new items · 5 KB estimated upload storage.")]
        [InlineData(3, 5120, 1, "3 new items · at least 5 KB estimated upload storage; 1 item has unknown size.")]
        [InlineData(2, 0, 2, "2 new items · backup size unknown until the device reports it.")]
        [InlineData(0, 0, 0, "No new camera media needs backup storage.")]
        public void Camera_backup_destination_storage_estimate_display_summarizes_pending_upload_size(
            int pendingCount,
            long knownSizeBytes,
            int unknownSizeCount,
            string expectedSummary)
        {
            CottonCameraBackupDestinationStorageEstimateDisplayState display =
                CottonCameraBackupDestinationStorageEstimateDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()),
                    CottonCameraBackupMediaAccessDisplayState.Create(CottonCameraBackupMediaAccessState.Allowed),
                    new CottonCameraBackupDestinationStorageEstimate(
                        pendingCount,
                        knownSizeBytes,
                        unknownSizeCount),
                    isCurrent: true);

            Assert.Equal(expectedSummary, display.SummaryText);
        }

        [Fact]
        public void Camera_backup_health_display_stays_honest_before_activity_exists()
        {
            CottonCameraBackupHealthDisplayState display =
                CottonCameraBackupHealthDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()),
                    CottonCameraBackupHealthSnapshot.Empty);

            Assert.Equal("Backup health", display.Title);
            Assert.Equal(
                "No backup activity yet.",
                display.StatusText);
            Assert.Equal(string.Empty, display.CountsText);
            Assert.False(display.IsBlocked);
            Assert.False(display.HasActivity);
        }

        [Fact]
        public void Camera_backup_health_prompts_for_destination_without_zero_count_noise()
        {
            CottonCameraBackupHealthDisplayState display =
                CottonCameraBackupHealthDisplayState.Create(
                    CottonCameraBackupSettings.Default,
                    CottonCameraBackupHealthSnapshot.Empty);

            Assert.Equal("Choose a destination to see backup activity.", display.StatusText);
            Assert.Equal(string.Empty, display.CountsText);
            Assert.True(display.IsBlocked);
            Assert.False(display.HasActivity);
        }

        [Fact]
        public void Camera_backup_health_reports_only_nonzero_activity_counts()
        {
            CottonCameraBackupHealthDisplayState display =
                CottonCameraBackupHealthDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()),
                    new CottonCameraBackupHealthSnapshot(2, 0, 1, 0));

            Assert.Equal("Some items need attention.", display.StatusText);
            Assert.Equal("2 pending · 1 failed", display.CountsText);
            Assert.True(display.IsBlocked);
            Assert.True(display.HasActivity);
        }

        [Theory]
        [InlineData(2, 0, 0, 0, "Items are waiting to upload.", "2 pending", false)]
        [InlineData(0, 3, 0, 0, "Backup activity is up to date.", "3 uploaded", false)]
        [InlineData(0, 0, 0, 4, "Some items need attention.", "4 blocked", true)]
        public void Camera_backup_health_status_matches_the_nonzero_activity(
            int pendingCount,
            int uploadedCount,
            int failedCount,
            int blockedCount,
            string expectedStatus,
            string expectedCounts,
            bool expectedBlocked)
        {
            CottonCameraBackupHealthDisplayState display =
                CottonCameraBackupHealthDisplayState.Create(
                    CottonCameraBackupSettings.Default.WithDestination(CreateDestination()),
                    new CottonCameraBackupHealthSnapshot(
                        pendingCount,
                        uploadedCount,
                        failedCount,
                        blockedCount));

            Assert.Equal(expectedStatus, display.StatusText);
            Assert.Equal(expectedCounts, display.CountsText);
            Assert.Equal(expectedBlocked, display.IsBlocked);
            Assert.True(display.HasActivity);
        }

        [Theory]
        [InlineData(CottonCameraBackupMediaAccessState.NotRequested, "Allow media access before queueing camera backup.")]
        [InlineData(CottonCameraBackupMediaAccessState.Limited, "Automatic camera backup needs full media access.")]
        [InlineData(CottonCameraBackupMediaAccessState.Denied, "Allow media access before queueing camera backup.")]
        public void Camera_backup_queue_status_keeps_permission_blocks_honest(
            CottonCameraBackupMediaAccessState accessState,
            string expectedStatus)
        {
            CottonCameraBackupMediaAccessDisplayState display =
                CottonCameraBackupMediaAccessDisplayState.Create(accessState);

            Assert.Equal(expectedStatus, CottonCameraBackupQueueStatusText.CreateBlockedAccessStatus(display));
        }

        [Theory]
        [InlineData(0, 0, 0, true, "Choose a folder before queueing camera backup.")]
        [InlineData(1, 0, 0, false, "Queued 1 camera backup upload.")]
        [InlineData(2, 0, 0, false, "Queued 2 camera backup uploads.")]
        [InlineData(0, 0, 1, false, "Could not read 1 camera item.")]
        [InlineData(0, 0, 2, false, "Could not read 2 camera items.")]
        [InlineData(0, 1, 0, false, "Camera backup uploads are already queued.")]
        [InlineData(0, 0, 0, false, "No new camera backup items to queue.")]
        public void Camera_backup_queue_status_summarizes_enqueue_results(
            int queuedCount,
            int skippedExisting,
            int missingStreams,
            bool missingDestination,
            string expectedStatus)
        {
            var result = new CottonCameraBackupTransferEnqueueResult(
                scannedCount: queuedCount + skippedExisting + missingStreams,
                queuedCount,
                skippedExisting,
                missingStreams,
                missingDestination,
                queuedTransfers: []);

            Assert.Equal(expectedStatus, CottonCameraBackupQueueStatusText.CreateResultStatus(result));
        }

        private static CottonUploadDestinationSnapshot CreateDestination()
        {
            return new CottonUploadDestinationSnapshot(
                DestinationId,
                "Camera",
                "Files / Camera");
        }

        private static CottonCameraBackupCandidate CreateCandidate(
            string sourceId,
            CottonCameraBackupMediaKind kind)
        {
            string fileName = kind == CottonCameraBackupMediaKind.Photo ? "photo.jpg" : "video.mp4";
            string contentType = kind == CottonCameraBackupMediaKind.Photo ? "image/jpeg" : "video/mp4";
            long sizeBytes = kind == CottonCameraBackupMediaKind.Photo ? 1024 : 4096;
            return new CottonCameraBackupCandidate(
                new CottonCameraBackupMediaIdentity(
                    sourceId,
                    new DateTime(2026, 6, 19, 9, 0, 0, DateTimeKind.Utc),
                    sizeBytes),
                kind,
                fileName,
                contentType,
                new DateTime(2026, 6, 19, 8, 0, 0, DateTimeKind.Utc));
        }

        private class StubCameraBackupMediaSource : ICottonCameraBackupMediaSource
        {
            private readonly IReadOnlyList<CottonCameraBackupCandidate> _candidates;

            public StubCameraBackupMediaSource(params CottonCameraBackupCandidate[] candidates)
            {
                _candidates = candidates;
            }

            public Task<IReadOnlyList<CottonCameraBackupCandidate>> ListCandidatesAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(_candidates);
            }
        }
    }
}
