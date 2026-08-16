// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    internal static class AndroidMediaStorePathMapper
    {
        private const string ImagesFolderName = "Images";
        private const string VideosFolderName = "Videos";

        public static string CreateParentPath(
            AndroidMediaStoreCollectionKind collectionKind,
            long mediaId,
            string? scopedStorageRelativePath)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                return (scopedStorageRelativePath ?? string.Empty).Trim().Trim('/');
            }

            string collectionName = collectionKind switch
            {
                AndroidMediaStoreCollectionKind.Images => ImagesFolderName,
                AndroidMediaStoreCollectionKind.Videos => VideosFolderName,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(collectionKind),
                    collectionKind,
                    "Android media collection is not supported."),
            };
            return $"{collectionName}/{mediaId.ToString(CultureInfo.InvariantCulture)}";
        }

        public static string CreateRawFilePath(string parentPath, string displayName)
        {
            string normalizedDisplayName = CreateProblemDisplayName(displayName);
            return string.IsNullOrWhiteSpace(parentPath)
                ? normalizedDisplayName
                : $"{parentPath}/{normalizedDisplayName}";
        }

        public static bool TryCreateFilePath(
            string parentPath,
            string displayName,
            [NotNullWhen(true)] out string? relativePath)
        {
            try
            {
                relativePath = CottonSyncRelativePath.CreateFilePath(parentPath, displayName);
                return true;
            }
            catch (ArgumentException)
            {
                relativePath = null;
                return false;
            }
        }

        public static void AddParentFolders(
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> items,
            string relativePath,
            DateTime updatedAtUtc)
        {
            string[] segments = relativePath.Split('/');
            string parentPath = string.Empty;
            for (int index = 0; index < segments.Length - 1; index++)
            {
                string folderName = segments[index];
                parentPath = CottonSyncRelativePath.CreateChildFolderPath(parentPath, folderName);
                items.TryAdd(
                    parentPath,
                    CottonDeviceToCloudLocalItemSnapshot.CreateFolder(
                        folderName,
                        parentPath,
                        updatedAtUtc));
            }
        }

        public static CottonDeviceToCloudLocalProblemSnapshot CreateInvalidNameProblem(
            string displayName,
            string relativePath)
        {
            return new CottonDeviceToCloudLocalProblemSnapshot(
                CottonDeviceToCloudLocalProblemKind.InvalidCloudName,
                CottonFileBrowserEntryType.File,
                CreateProblemDisplayName(displayName),
                relativePath,
                CoreResources.UnsyncableName);
        }

        private static string CreateProblemDisplayName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? CoreResources.UnnamedName : displayName.Trim();
        }
    }
}
#endif
