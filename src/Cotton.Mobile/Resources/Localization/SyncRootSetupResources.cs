// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using System.Resources;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Resources.Localization
{
    public static class SyncRootSetupResources
    {
        private static readonly ResourceManager ResourceManagerInstance = new(typeof(SyncRootSetupResources));

        public static string PageTitle => GetString(nameof(PageTitle));

        public static string AppBarTitle => GetString(nameof(AppBarTitle));

        public static string ContinueDescription => GetString(nameof(ContinueDescription));

        public static string Heading => GetString(nameof(Heading));

        public static string SupportingText => GetString(nameof(SupportingText));

        public static string UploadOnlyTitle => GetString(nameof(UploadOnlyTitle));

        public static string UploadOnlySupportingText => GetString(nameof(UploadOnlySupportingText));

        public static string BidirectionalTitle => GetString(nameof(BidirectionalTitle));

        public static string BidirectionalSupportingText => GetString(nameof(BidirectionalSupportingText));

        public static string CreateModeDescription(string title, bool isSelected)
        {
            string format = isSelected
                ? GetString("SelectedModeDescriptionFormat")
                : GetString("AvailableModeDescriptionFormat");
            return string.Format(CultureInfo.CurrentCulture, format, title.Trim());
        }

        public static string DeleteOriginalsTitle => GetString(nameof(DeleteOriginalsTitle));

        public static string DeleteOriginalsSupportingText => GetString(nameof(DeleteOriginalsSupportingText));

        public static string UnavailableMessage => GetString(nameof(UnavailableMessage));

        public static string AlreadyConfiguredMessage => GetString(nameof(AlreadyConfiguredMessage));

        public static string DirectionConflictMessage => GetString(nameof(DirectionConflictMessage));

        public static string CreateCreatedMessage(CottonSyncDirection direction, string cloudPath)
        {
            string format = direction switch
            {
                CottonSyncDirection.DeviceToCloud => GetString("UploadOnlyCreatedMessageFormat"),
                CottonSyncDirection.Bidirectional => GetString("BidirectionalCreatedMessageFormat"),
                CottonSyncDirection.CloudToDevice => throw new ArgumentException(
                    "Cloud-to-device sync cannot be created during setup.",
                    nameof(direction)),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported."),
            };
            return FormatCloudPath(format, cloudPath);
        }

        public static string CreateUpdatedMessage(CottonSyncDirection direction, string cloudPath)
        {
            string format = direction switch
            {
                CottonSyncDirection.DeviceToCloud => GetString("UploadOnlyUpdatedMessageFormat"),
                CottonSyncDirection.Bidirectional => GetString("BidirectionalUpdatedMessageFormat"),
                CottonSyncDirection.CloudToDevice => throw new ArgumentException(
                    "Cloud-to-device sync cannot be updated during setup.",
                    nameof(direction)),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported."),
            };
            return FormatCloudPath(format, cloudPath);
        }

        public static string CreateReconnectedMessage(string cloudPath)
        {
            return FormatCloudPath(GetString("ReconnectedMessageFormat"), cloudPath);
        }

        private static string GetString(string name)
        {
            return ResourceManagerInstance.GetString(name, CultureInfo.CurrentUICulture)
                ?? throw new InvalidOperationException($"Sync setup resource '{name}' is missing.");
        }

        private static string FormatCloudPath(string format, string cloudPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cloudPath);
            return string.Format(CultureInfo.CurrentCulture, format, cloudPath.Trim());
        }
    }
}
