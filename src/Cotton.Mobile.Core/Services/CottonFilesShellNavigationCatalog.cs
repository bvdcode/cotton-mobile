// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFilesShellNavigationCatalog
    {
        private static readonly IReadOnlyList<CottonFilesShellNavigationItem> Items =
        [
            new(
                CottonFilesShellNavigationDestination.Files,
                "Files",
                "Files"),
            new(
                CottonFilesShellNavigationDestination.Transfers,
                "Transfers",
                "Open transfers"),
            new(
                CottonFilesShellNavigationDestination.Inbox,
                "Inbox",
                "Open capture inbox"),
            new(
                CottonFilesShellNavigationDestination.Backup,
                "Backup",
                "Open camera backup"),
            new(
                CottonFilesShellNavigationDestination.Settings,
                "Settings",
                "Open account and settings"),
        ];

        public static IReadOnlyList<CottonFilesShellNavigationItem> CreateItems()
        {
            return Items;
        }

        public static CottonFilesShellNavigationItem Get(CottonFilesShellNavigationDestination destination)
        {
            return Items.Single(item => item.Destination == destination);
        }
    }
}
