// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupSetupDisplayState
    {
        private const string NoDestinationText = "No folder selected";

        private CottonCameraBackupSetupDisplayState(
            string destinationText,
            string executionStatusText,
            string localMediaRetentionText,
            bool isDestinationSelected,
            bool canEnableBackup)
        {
            DestinationText = destinationText;
            ExecutionStatusText = executionStatusText;
            LocalMediaRetentionText = localMediaRetentionText;
            IsDestinationSelected = isDestinationSelected;
            CanEnableBackup = canEnableBackup;
        }

        public string DestinationText { get; }

        public string ExecutionStatusText { get; }

        public string LocalMediaRetentionText { get; }

        public bool IsDestinationSelected { get; }

        public bool CanEnableBackup { get; }

        public static CottonCameraBackupSetupDisplayState Create(CottonCameraBackupSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            string destinationText = settings.Destination?.Path ?? NoDestinationText;
            string executionStatusText = !settings.HasDestination
                ? string.Empty
                : settings.IsEnabled
                    ? "Camera backup is on."
                    : "Camera backup is off.";
            string localMediaRetentionText =
                CottonCameraBackupLocalMediaRetentionPolicy.Mvp.SetupSummaryText;

            return new CottonCameraBackupSetupDisplayState(
                destinationText,
                executionStatusText,
                localMediaRetentionText,
                settings.HasDestination,
                settings.CanRunBackup);
        }
    }
}
