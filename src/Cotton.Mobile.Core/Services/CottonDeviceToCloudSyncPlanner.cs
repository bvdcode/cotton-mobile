// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonDeviceToCloudSyncPlanner
    {
        public static CottonDeviceToCloudSyncPlanSnapshot Create(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonUploadReceiptSnapshot> uploadReceipts)
        {
            ValidateInput(root, localContent, remoteContent, uploadReceipts);

            CottonDeviceToCloudSyncIndex index = new(localContent, remoteContent, uploadReceipts);
            CottonDeviceToCloudSyncItemPlanner itemPlanner = new(root, index);
            HashSet<string> requiredFolderPaths = new(StringComparer.OrdinalIgnoreCase);
            List<CottonDeviceToCloudSyncPlanItem> fileItems = [];
            foreach (CottonDeviceToCloudLocalItemSnapshot localFile in localContent.Items
                .Where(item => item.ItemType == CottonFileBrowserEntryType.File)
                .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                CottonDeviceToCloudSyncPlanItem fileItem = itemPlanner.CreateLocalFileItem(localFile);
                if (fileItem.RequiresUpload
                    && !index.TryCollectRequiredFolders(
                        fileItem,
                        requiredFolderPaths,
                        out CottonDeviceToCloudRemoteItemSnapshot? conflictingParent))
                {
                    fileItem = CottonDeviceToCloudSyncPlanItemFactory.CreateParentConflict(
                        fileItem,
                        conflictingParent);
                }

                fileItems.Add(fileItem);
            }

            List<CottonDeviceToCloudSyncPlanItem> items = [.. localContent.Problems
                .OrderBy(problem => problem.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(CottonDeviceToCloudSyncPlanItemFactory.CreateLocalProblem)];
            items.AddRange(requiredFolderPaths
                .OrderBy(CottonDeviceToCloudSyncIndex.GetPathDepth)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => CottonDeviceToCloudSyncPlanItemFactory.CreateRequiredFolder(
                    index.LocalByPath[path],
                    index.RemoteByPath)));
            items.AddRange(fileItems);

            return new CottonDeviceToCloudSyncPlanSnapshot(
                root.Id,
                remoteContent.FolderId,
                remoteContent.FolderName,
                items);
        }

        private static void ValidateInput(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonUploadReceiptSnapshot> uploadReceipts)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(localContent);
            ArgumentNullException.ThrowIfNull(remoteContent);
            ArgumentNullException.ThrowIfNull(uploadReceipts);

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Upload-only sync requires a ready sync root.");
            }

            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("Upload-only sync requires a device-to-cloud sync root.");
            }

            if (root.CloudFolder.FolderId != remoteContent.FolderId)
            {
                throw new ArgumentException(
                    "Remote folder content does not match the sync root cloud folder.",
                    nameof(remoteContent));
            }
        }
    }
}
