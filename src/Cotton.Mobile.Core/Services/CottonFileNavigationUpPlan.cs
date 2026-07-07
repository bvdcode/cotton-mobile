// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileNavigationUpPlan
    {
        private CottonFileNavigationUpPlan(
            bool canNavigate,
            CottonFolderHandle? targetFolder,
            IReadOnlyList<CottonFolderHandle> navigationAfterNavigate)
        {
            CanNavigate = canNavigate;
            TargetFolder = targetFolder;
            NavigationAfterNavigate = navigationAfterNavigate;
        }

        public bool CanNavigate { get; }

        public bool IsRootTarget => CanNavigate && TargetFolder is null;

        public CottonFolderHandle? TargetFolder { get; }

        public IReadOnlyList<CottonFolderHandle> NavigationAfterNavigate { get; }

        public static CottonFileNavigationUpPlan None { get; } =
            new(false, null, Array.Empty<CottonFolderHandle>());

        public static CottonFileNavigationUpPlan Root() =>
            new(true, null, Array.Empty<CottonFolderHandle>());

        public static CottonFileNavigationUpPlan Folder(
            CottonFolderHandle targetFolder,
            IReadOnlyList<CottonFolderHandle> navigationAfterNavigate)
        {
            ArgumentNullException.ThrowIfNull(targetFolder);
            ArgumentNullException.ThrowIfNull(navigationAfterNavigate);

            return new CottonFileNavigationUpPlan(
                true,
                targetFolder,
                navigationAfterNavigate.ToArray());
        }
    }
}
