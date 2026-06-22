// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileUploadStatusText
    {
        public const string FileSourceKind = "file";
        public const string PhotoSourceKind = "photo";
        public const string VideoSourceKind = "video";

        public static string CreateStartingStatus(string sourceKind, string fileName, int importCount = 1)
        {
            string name = NormalizeFileName(fileName);
            return NormalizeSourceKind(sourceKind) switch
            {
                PhotoSourceKind => $"Importing {FormatImportCount(PhotoSourceKind, importCount)}: {name}...",
                VideoSourceKind => $"Importing {FormatImportCount(VideoSourceKind, importCount)}: {name}...",
                _ => $"Uploading {name}...",
            };
        }

        public static string CreateCompletedStatus(
            string sourceKind,
            string fileName,
            string? destinationPath = null,
            int importCount = 1)
        {
            string name = NormalizeFileName(fileName);
            string destination = string.IsNullOrWhiteSpace(destinationPath)
                ? string.Empty
                : $" to {destinationPath.Trim()}";
            return NormalizeSourceKind(sourceKind) switch
            {
                PhotoSourceKind => $"Imported {FormatImportCount(PhotoSourceKind, importCount)}: {name}{destination}.",
                VideoSourceKind => $"Imported {FormatImportCount(VideoSourceKind, importCount)}: {name}{destination}.",
                _ => $"Uploaded {name}{destination}.",
            };
        }

        private static string FormatImportCount(string sourceKind, int importCount)
        {
            int count = Math.Max(1, importCount);
            string noun = NormalizeSourceKind(sourceKind) == VideoSourceKind ? "video" : "photo";
            return count == 1 ? $"1 {noun}" : $"{count:N0} {noun}s";
        }

        private static string NormalizeFileName(string fileName)
        {
            return string.IsNullOrWhiteSpace(fileName) ? CottonFileUploadSourceSnapshot.DefaultFileName : fileName.Trim();
        }

        private static string NormalizeSourceKind(string sourceKind)
        {
            return string.IsNullOrWhiteSpace(sourceKind) ? FileSourceKind : sourceKind.Trim().ToLowerInvariant();
        }
    }
}
