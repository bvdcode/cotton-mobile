// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRecursiveRemoteContentLoader(
        ICottonDeviceToCloudRemoteFolderContentSource remoteFolderContentSource)
    {
        private readonly ICottonDeviceToCloudRemoteFolderContentSource _remoteFolderContentSource =
            remoteFolderContentSource ?? throw new ArgumentNullException(nameof(remoteFolderContentSource));

        public async Task<CottonDeviceToCloudRemoteContentSnapshot> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);

            List<CottonDeviceToCloudRemoteItemSnapshot> items = [];
            Queue<(CottonFolderHandle Folder, string RelativePath)> folders = [];
            HashSet<Guid> visitedFolderIds = [];

            folders.Enqueue((root.CloudFolder.ToFolderHandle(), string.Empty));
            while (folders.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (CottonFolderHandle folder, string folderRelativePath) = folders.Dequeue();
                if (!visitedFolderIds.Add(folder.Id))
                {
                    continue;
                }

                CottonFolderContent content = await _remoteFolderContentSource
                    .LoadAsync(instanceUri, folder, cancellationToken)
                    .ConfigureAwait(false);
                foreach (CottonFileBrowserEntry entry in content.Entries)
                {
                    string relativePath = CreateRelativePath(folderRelativePath, entry);
                    items.Add(new CottonDeviceToCloudRemoteItemSnapshot(entry, relativePath));
                    if (entry.Type == CottonFileBrowserEntryType.Folder)
                    {
                        folders.Enqueue((new CottonFolderHandle(entry.Id, entry.Name), relativePath));
                    }
                }
            }

            return new CottonDeviceToCloudRemoteContentSnapshot(
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                items);
        }

        private static string CreateRelativePath(string folderRelativePath, CottonFileBrowserEntry entry)
        {
            return entry.Type switch
            {
                CottonFileBrowserEntryType.Folder =>
                    CottonSyncRelativePath.CreateChildFolderPath(folderRelativePath, entry.Name),
                CottonFileBrowserEntryType.File =>
                    CottonSyncRelativePath.CreateFilePath(folderRelativePath, entry.Name),
                _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Type, "Remote item type is not supported."),
            };
        }
    }
}
