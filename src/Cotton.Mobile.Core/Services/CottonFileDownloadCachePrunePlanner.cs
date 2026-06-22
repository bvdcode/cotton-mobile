// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileDownloadCachePrunePlanner
    {
        public static IReadOnlyList<string> SelectFilesToDelete(
            IReadOnlyCollection<CottonFileDownloadCacheEntry> entries,
            long maxCacheBytes,
            string? protectedPath,
            IReadOnlyCollection<string> protectedDirectories)
        {
            ArgumentNullException.ThrowIfNull(entries);
            ArgumentNullException.ThrowIfNull(protectedDirectories);
            if (maxCacheBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCacheBytes), "Cache size must be positive.");
            }

            string? normalizedProtectedPath = NormalizePath(protectedPath);
            HashSet<string> normalizedProtectedDirectories = protectedDirectories
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => Path.GetFullPath(path))
                .ToHashSet(StringComparer.Ordinal);
            long totalBytes = entries.Sum(entry => entry.SizeBytes);
            var deletePaths = new List<string>();

            foreach (CottonFileDownloadCacheEntry entry in entries
                .OrderBy(entry => entry.ActivityUtc)
                .ThenBy(entry => entry.Path, StringComparer.Ordinal))
            {
                if (!CottonSensitiveFileCachePolicy.RequiresSensitiveCacheEviction(entry)
                    || IsProtected(entry.Path, normalizedProtectedPath, normalizedProtectedDirectories))
                {
                    continue;
                }

                deletePaths.Add(entry.Path);
                totalBytes -= entry.SizeBytes;
            }

            HashSet<string> selectedPaths = deletePaths.ToHashSet(StringComparer.Ordinal);
            foreach (CottonFileDownloadCacheEntry entry in entries
                .OrderBy(entry => entry.ActivityUtc)
                .ThenBy(entry => entry.Path, StringComparer.Ordinal))
            {
                if (totalBytes <= maxCacheBytes)
                {
                    break;
                }

                if (selectedPaths.Contains(entry.Path)
                    || IsProtected(entry.Path, normalizedProtectedPath, normalizedProtectedDirectories))
                {
                    continue;
                }

                deletePaths.Add(entry.Path);
                selectedPaths.Add(entry.Path);
                totalBytes -= entry.SizeBytes;
            }

            return deletePaths;
        }

        private static string? NormalizePath(string? path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
        }

        private static bool IsProtected(
            string path,
            string? normalizedProtectedPath,
            IReadOnlySet<string> normalizedProtectedDirectories)
        {
            string normalizedPath = Path.GetFullPath(path);
            if (normalizedProtectedPath is not null
                && string.Equals(normalizedPath, normalizedProtectedPath, StringComparison.Ordinal))
            {
                return true;
            }

            return normalizedProtectedDirectories.Any(directory => IsPathInsideDirectory(normalizedPath, directory));
        }

        private static bool IsPathInsideDirectory(string path, string directory)
        {
            string relativePath = Path.GetRelativePath(directory, path);
            return !Path.IsPathRooted(relativePath)
                && !string.Equals(relativePath, ".", StringComparison.Ordinal)
                && !string.Equals(relativePath, "..", StringComparison.Ordinal)
                && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
        }
    }
}
