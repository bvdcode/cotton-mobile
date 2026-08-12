// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonBidirectionalSyncPlanner
    {
        public static CottonBidirectionalSyncPlanSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonSyncedFileSnapshot> manifestFiles)
        {
            ValidateInput(root, localContent, remoteContent, manifestFiles);

            CottonBidirectionalSyncIndex index = new(localContent, remoteContent, manifestFiles);
            CottonBidirectionalSyncItemPlanner itemPlanner = new(index);
            HashSet<Guid> handledRemoteIds = [];
            HashSet<string> handledManifestPaths = new(StringComparer.OrdinalIgnoreCase);
            List<CottonBidirectionalSyncPlanItem> items = [.. localContent.Problems
                .OrderBy(problem => problem.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(CottonBidirectionalSyncPlanItemFactory.CreateLocalProblem)];

            foreach (CottonDeviceToCloudLocalItemSnapshot localItem in localContent.Items
                .OrderBy(item => item.ItemType == CottonFileBrowserEntryType.Folder ? 0 : 1)
                .ThenBy(item => CottonBidirectionalSyncIndex.GetPathDepth(item.RelativePath))
                .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                CottonBidirectionalSyncPlanItem item = itemPlanner.CreateLocalItem(
                    localItem,
                    handledManifestPaths);
                items.Add(item);
                AddCloudItemId(item, handledRemoteIds);
            }

            foreach (CottonSyncedFileSnapshot manifestItem in index.ManifestByPath.Values
                .Where(item => !handledManifestPaths.Contains(item.RelativePath))
                .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.FileId))
            {
                CottonBidirectionalSyncPlanItem item = itemPlanner.CreateMissingLocalItem(manifestItem);
                items.Add(item);
                AddCloudItemId(item, handledRemoteIds);
            }

            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items
                .Where(item => !handledRemoteIds.Contains(item.Entry.Id))
                .OrderBy(item => item.Entry.Type == CottonFileBrowserEntryType.Folder ? 0 : 1)
                .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                if (!index.LocalByPath.ContainsKey(remoteItem.RelativePath))
                {
                    items.Add(CottonBidirectionalSyncItemPlanner.CreateRemoteOnlyItem(remoteItem));
                }
            }

            return new CottonBidirectionalSyncPlanSnapshot(
                root.Id,
                remoteContent.FolderId,
                remoteContent.FolderName,
                items);
        }

        private static void ValidateInput(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonSyncedFileSnapshot> manifestFiles)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(localContent);
            ArgumentNullException.ThrowIfNull(remoteContent);
            ArgumentNullException.ThrowIfNull(manifestFiles);

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Bidirectional sync requires a ready sync root.");
            }

            if (root.Direction != CottonSyncDirection.Bidirectional)
            {
                throw new InvalidOperationException("Bidirectional sync requires a bidirectional sync root.");
            }

            if (root.CloudFolder.FolderId != remoteContent.FolderId)
            {
                throw new ArgumentException(
                    "Remote folder content does not match the sync root cloud folder.",
                    nameof(remoteContent));
            }
        }

        private static void AddCloudItemId(
            CottonBidirectionalSyncPlanItem item,
            HashSet<Guid> handledRemoteIds)
        {
            if (item.CloudItemId.HasValue)
            {
                handledRemoteIds.Add(item.CloudItemId.Value);
            }
        }
    }
}
