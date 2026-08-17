// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using System.Resources;

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

        public static string FolderTitle => GetString(nameof(FolderTitle));

        public static string FolderSupportingText => GetString(nameof(FolderSupportingText));

        public static string MediaTitle => GetString(nameof(MediaTitle));

        public static string MediaSupportingText => GetString(nameof(MediaSupportingText));

        public static string MediaAlbumPageTitle => GetString(nameof(MediaAlbumPageTitle));

        public static string MediaAlbumAppBarTitle => GetString(nameof(MediaAlbumAppBarTitle));

        public static string MediaAlbumHeading => GetString(nameof(MediaAlbumHeading));

        public static string MediaAlbumSupportingText => GetString(nameof(MediaAlbumSupportingText));

        public static string MediaAlbumItemCountFormat => GetString(nameof(MediaAlbumItemCountFormat));

        public static string MediaAlbumSingleItem => GetString(nameof(MediaAlbumSingleItem));

        public static string MediaAlbumsDisplayNameFormat => GetString(nameof(MediaAlbumsDisplayNameFormat));

        public static string UnnamedMediaAlbum => GetString(nameof(UnnamedMediaAlbum));

        public static string NoMediaAlbumsTitle => GetString(nameof(NoMediaAlbumsTitle));

        public static string NoMediaAlbumsSupportingText => GetString(nameof(NoMediaAlbumsSupportingText));

        public static string MediaAccessRequiredTitle => GetString(nameof(MediaAccessRequiredTitle));

        public static string MediaAccessRequiredMessage => GetString(nameof(MediaAccessRequiredMessage));

        public static string CreateMediaAlbumItemCount(int itemCount)
        {
            if (itemCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), "Media album item count must be positive.");
            }

            return itemCount == 1
                ? GetString(nameof(MediaAlbumSingleItem))
                : string.Format(
                    CultureInfo.CurrentCulture,
                    GetString(nameof(MediaAlbumItemCountFormat)),
                    itemCount);
        }

        public static string CreateMediaAlbumsDisplayName(int albumCount)
        {
            if (albumCount <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(albumCount), "Multiple media albums are required.");
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                GetString(nameof(MediaAlbumsDisplayNameFormat)),
                albumCount);
        }

        public static string CreateSourceDescription(string title, bool isSelected)
        {
            string format = isSelected
                ? GetString("SelectedSourceDescriptionFormat")
                : GetString("AvailableSourceDescriptionFormat");
            return string.Format(CultureInfo.CurrentCulture, format, title.Trim());
        }

        public static string DeleteOriginalsTitle => GetString(nameof(DeleteOriginalsTitle));

        public static string DeleteOriginalsSupportingText => GetString(nameof(DeleteOriginalsSupportingText));

        public static string UnavailableMessage => GetString(nameof(UnavailableMessage));

        public static string AlreadyConfiguredMessage => GetString(nameof(AlreadyConfiguredMessage));

        public static string SourceConflictMessage => GetString(nameof(SourceConflictMessage));

        public static string CreateCreatedMessage(string cloudPath)
        {
            return FormatCloudPath(GetString("CreatedMessageFormat"), cloudPath);
        }

        public static string CreateUpdatedMessage(string cloudPath)
        {
            return FormatCloudPath(GetString("UpdatedMessageFormat"), cloudPath);
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
