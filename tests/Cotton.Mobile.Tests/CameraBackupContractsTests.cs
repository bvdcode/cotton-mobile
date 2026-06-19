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

            Assert.Equal("No folder selected", missingDestination.DestinationText);
            Assert.Equal("Choose a folder before camera backup can run.", missingDestination.ExecutionStatusText);
            Assert.False(missingDestination.CanEnableBackup);

            Assert.Equal("Setup saved. Background backup is not running yet.", withDestination.ExecutionStatusText);
            Assert.False(withDestination.CanEnableBackup);
        }

        [Fact]
        public void Camera_backup_policy_summary_tracks_media_network_and_charging_choices()
        {
            CottonCameraBackupSettings settings = CottonCameraBackupSettings.Default
                .WithPhotosOnly(false)
                .WithAllowCellular(true)
                .WithChargingOnly(true);

            CottonCameraBackupSetupDisplayState display = CottonCameraBackupSetupDisplayState.Create(settings);

            Assert.Equal("Photos and videos, cellular allowed, while charging.", display.PolicySummaryText);
        }

        [Fact]
        public void Camera_backup_rejects_enabled_state_before_durable_queue_is_available()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new CottonCameraBackupSettings(
                    isEnabled: true,
                    CreateDestination(),
                    photosOnly: true,
                    wifiOnly: true,
                    allowCellular: false,
                    chargingOnly: false));
        }

        private static CottonUploadDestinationSnapshot CreateDestination()
        {
            return new CottonUploadDestinationSnapshot(
                DestinationId,
                "Camera",
                "Files / Camera");
        }
    }
}
