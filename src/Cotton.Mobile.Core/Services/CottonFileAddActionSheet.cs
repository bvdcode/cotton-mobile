// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileAddActionSheet
    {
        public const string CancelAction = "Cancel";
        public const string NewFolderAction = "New folder";
        public const string UploadAction = "Upload...";
        public const string UploadFileAction = "Upload file";
        public const string ScanDocumentAction = "Scan document";
        public const string UploadPhotoAction = "Upload photo";
        public const string UploadVideoAction = "Upload video";
        public const string UploadPhotoToFolderAction = "Upload photo to folder";
        public const string UploadVideoToFolderAction = "Upload video to folder";

        private static readonly string[] ActionsWithDocumentScan =
        [
            NewFolderAction,
            UploadAction,
            ScanDocumentAction,
        ];

        private static readonly string[] ActionsWithoutDocumentScan =
        [
            NewFolderAction,
            UploadAction,
        ];

        private static readonly string[] UploadActions =
        [
            UploadFileAction,
            UploadPhotoAction,
            UploadVideoAction,
        ];

        public static IReadOnlyList<string> CreateActions(bool canScanDocument = true)
        {
            return canScanDocument ? ActionsWithDocumentScan : ActionsWithoutDocumentScan;
        }

        public static IReadOnlyList<string> CreateUploadActions()
        {
            return UploadActions;
        }

        public static string CreateTitle(CottonFolderHandle folder)
        {
            ArgumentNullException.ThrowIfNull(folder);

            return string.IsNullOrWhiteSpace(folder.Name)
                ? "Add"
                : $"Add to {folder.Name.Trim()}";
        }
    }
}
