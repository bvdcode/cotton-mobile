// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootListDisplayState
    {
        private CottonSyncRootListDisplayState(IReadOnlyList<CottonSyncRootListItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            Items = items;
            SummaryText = CreateSummaryText(items.Count);
        }

        public IReadOnlyList<CottonSyncRootListItem> Items { get; }

        public string SummaryText { get; }

        public bool HasItems => Items.Count > 0;

        public bool CanRunAny => Items.Any(item => item.CanRunNow);

        public bool IsEmptyVisible => !HasItems;

        public static CottonSyncRootListDisplayState Create(IReadOnlyList<CottonSyncRootSnapshot> roots)
        {
            return Create(
                roots,
                new HashSet<Guid>(),
                new Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>());
        }

        public static CottonSyncRootListDisplayState Create(
            IReadOnlyList<CottonSyncRootSnapshot> roots,
            IReadOnlySet<Guid> pausedRootIds)
        {
            return Create(
                roots,
                pausedRootIds,
                new Dictionary<Guid, CottonAutomaticSyncRootStatusSnapshot>());
        }

        public static CottonSyncRootListDisplayState Create(
            IReadOnlyList<CottonSyncRootSnapshot> roots,
            IReadOnlySet<Guid> pausedRootIds,
            IReadOnlyDictionary<Guid, CottonAutomaticSyncRootStatusSnapshot> automaticStatuses)
        {
            ArgumentNullException.ThrowIfNull(roots);
            ArgumentNullException.ThrowIfNull(pausedRootIds);
            ArgumentNullException.ThrowIfNull(automaticStatuses);

            CottonSyncRootSnapshot[] orderedRoots = [.. roots
                .OrderBy(root => root.CloudFolder.Path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(root => root.CloudFolder.FolderName, StringComparer.OrdinalIgnoreCase)];
            CottonSyncRootListItem[] items = [.. orderedRoots.Select((root, index) =>
                new CottonSyncRootListItem(
                    root,
                    pausedRootIds.Contains(root.Id),
                    automaticStatuses.GetValueOrDefault(root.Id),
                    isDividerVisible: index < orderedRoots.Length - 1))];
            return new CottonSyncRootListDisplayState(items);
        }

        private static string CreateSummaryText(int count)
        {
            return count switch
            {
                0 => CoreResources.NoFoldersSyncing,
                1 => CoreResources.OneFolderSyncing,
                _ when count > 1 => CoreResources.Format(CoreResources.FoldersSyncingFormat, count),
                _ => throw new ArgumentOutOfRangeException(nameof(count), count, "Sync root count cannot be negative."),
            };
        }
    }
}
