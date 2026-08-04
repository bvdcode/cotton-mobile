// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonDeviceToCloudRemoteFolderIndex
    {
        private const char RelativePathSeparator = '/';

        private readonly Dictionary<string, CottonFolderHandle> _foldersByPath;

        public CottonDeviceToCloudRemoteFolderIndex(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(plan);

            _foldersByPath = new Dictionary<string, CottonFolderHandle>(StringComparer.OrdinalIgnoreCase)
            {
                [string.Empty] = root.CloudFolder.ToFolderHandle(),
            };

            foreach (CottonDeviceToCloudSyncPlanItem item in plan.Items)
            {
                if (item.Action != CottonDeviceToCloudSyncActionKind.KeepExistingFolder
                    || item.TargetType != CottonFileBrowserEntryType.Folder
                    || !item.CloudItemId.HasValue)
                {
                    continue;
                }

                _foldersByPath[item.RelativePath] = new CottonFolderHandle(
                    item.CloudItemId.Value,
                    item.DisplayName);
            }
        }

        public CottonFolderHandle ResolveParent(CottonDeviceToCloudSyncPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            string parentPath = GetParentPath(item.RelativePath);
            if (_foldersByPath.TryGetValue(parentPath, out CottonFolderHandle? parentFolder))
            {
                return parentFolder;
            }

            throw new InvalidOperationException("Device-to-cloud sync parent folder is not available.");
        }

        public void AddCreatedFolder(
            CottonDeviceToCloudSyncPlanItem item,
            CottonFileBrowserEntry createdFolder)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(createdFolder);
            if (createdFolder.Type != CottonFileBrowserEntryType.Folder)
            {
                throw new InvalidOperationException("Device-to-cloud folder creation returned a non-folder item.");
            }

            if (!string.Equals(createdFolder.Name, item.DisplayName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Device-to-cloud folder creation returned a different folder name.");
            }

            _foldersByPath[item.RelativePath] = new CottonFolderHandle(createdFolder.Id, createdFolder.Name);
        }

        private static string GetParentPath(string relativePath)
        {
            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(relativePath, nameof(relativePath));
            int separatorIndex = normalizedPath.LastIndexOf(RelativePathSeparator);
            return separatorIndex < 0 ? string.Empty : normalizedPath[..separatorIndex];
        }
    }
}
