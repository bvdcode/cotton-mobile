// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileNavigationPlanner
    {
        public static CottonFileNavigationUpPlan CreateNavigateUpPlan(
            CottonFolderHandle? currentFolder,
            IReadOnlyList<CottonFolderHandle> navigation)
        {
            ArgumentNullException.ThrowIfNull(navigation);

            if (navigation.Count == 0)
            {
                return currentFolder is null
                    ? CottonFileNavigationUpPlan.None
                    : CottonFileNavigationUpPlan.Root();
            }

            var remainingNavigation = navigation.Take(navigation.Count - 1).ToArray();
            return CottonFileNavigationUpPlan.Folder(
                navigation[navigation.Count - 1],
                remainingNavigation);
        }

        public static IReadOnlyList<CottonFolderHandle> CreateNavigationAfterOpenFolder(
            CottonFolderHandle? currentFolder,
            IReadOnlyList<CottonFolderHandle> navigation,
            bool isCurrentRoot)
        {
            ArgumentNullException.ThrowIfNull(navigation);

            if (currentFolder is null || isCurrentRoot)
            {
                return navigation.ToArray();
            }

            return navigation.Append(currentFolder).ToArray();
        }

        public static IReadOnlyList<string> CreatePathSegments(
            string rootName,
            IReadOnlyList<CottonFolderHandle> navigation,
            string currentFolderName)
        {
            ArgumentNullException.ThrowIfNull(navigation);

            string normalizedRootName = string.IsNullOrWhiteSpace(rootName) ? "Files" : rootName.Trim();
            string normalizedCurrentFolderName = string.IsNullOrWhiteSpace(currentFolderName)
                ? normalizedRootName
                : currentFolderName.Trim();

            return new[] { normalizedRootName }
                .Concat(navigation.Select(folder => folder.Name))
                .Append(normalizedCurrentFolderName)
                .ToArray();
        }
    }
}
