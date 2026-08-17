// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRootPathText
    {
        private const string PathSeparator = " / ";
        private const string DestinationSeparator = " → ";

        public static string Create(string folderName, string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(folderName);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            string normalizedFolderName = folderName.Trim();
            string[] segments = path
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length <= 1)
            {
                return normalizedFolderName;
            }

            string visiblePath = string.Join(PathSeparator, segments.Skip(1));
            return string.Equals(normalizedFolderName, visiblePath, StringComparison.OrdinalIgnoreCase)
                ? normalizedFolderName
                : string.Concat(normalizedFolderName, DestinationSeparator, visiblePath);
        }
    }
}
