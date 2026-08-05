// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonDeviceToCloudSyncIndex
    {
        private const char RelativePathSeparator = '/';

        public CottonDeviceToCloudSyncIndex(
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonUploadReceiptSnapshot> uploadReceipts)
        {
            LocalByPath = CreateLocalItemMap(localContent);
            RemoteByPath = CreateRemotePathMap(remoteContent);
            ReceiptsBySource = CreateReceiptSourceMap(uploadReceipts);
            RemoteByOperation = CreateRemoteOperationMap(remoteContent);
        }

        public IReadOnlyDictionary<string, CottonDeviceToCloudLocalItemSnapshot> LocalByPath { get; }

        public IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> RemoteByPath { get; }

        public IReadOnlyDictionary<string, CottonUploadReceiptSnapshot> ReceiptsBySource { get; }

        public IReadOnlyDictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> RemoteByOperation { get; }

        public bool TryCollectRequiredFolders(
            CottonDeviceToCloudSyncPlanItem fileItem,
            ISet<string> requiredFolderPaths,
            out CottonDeviceToCloudRemoteItemSnapshot? conflictingParent)
        {
            foreach (string parentPath in GetParentPaths(fileItem.RelativePath))
            {
                if (!LocalByPath.TryGetValue(
                    parentPath,
                    out CottonDeviceToCloudLocalItemSnapshot? localFolder)
                    || localFolder.ItemType != CottonFileBrowserEntryType.Folder)
                {
                    conflictingParent = null;
                    return false;
                }

                if (RemoteByPath.TryGetValue(
                    parentPath,
                    out CottonDeviceToCloudRemoteItemSnapshot? remoteFolder)
                    && remoteFolder.Entry.Type != CottonFileBrowserEntryType.Folder)
                {
                    conflictingParent = remoteFolder;
                    return false;
                }

                requiredFolderPaths.Add(parentPath);
            }

            conflictingParent = null;
            return true;
        }

        public static int GetPathDepth(string relativePath)
        {
            return relativePath.Count(character => character == RelativePathSeparator);
        }

        private static IReadOnlyList<string> GetParentPaths(string relativePath)
        {
            var paths = new List<string>();
            string currentPath = relativePath;
            while (currentPath.LastIndexOf(RelativePathSeparator) is int separatorIndex && separatorIndex >= 0)
            {
                currentPath = currentPath[..separatorIndex];
                paths.Add(currentPath);
            }

            paths.Reverse();
            return paths;
        }

        private static Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> CreateLocalItemMap(
            CottonDeviceToCloudLocalContentSnapshot localContent)
        {
            var result = new Dictionary<string, CottonDeviceToCloudLocalItemSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (CottonDeviceToCloudLocalItemSnapshot localItem in localContent.Items)
            {
                if (!result.TryAdd(localItem.RelativePath, localItem))
                {
                    throw new ArgumentException(
                        "Upload-only local content contains duplicate relative paths.",
                        nameof(localContent));
                }
            }

            return result;
        }

        private static Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> CreateRemotePathMap(
            CottonDeviceToCloudRemoteContentSnapshot remoteContent)
        {
            var result = new Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot>(StringComparer.OrdinalIgnoreCase);
            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items)
            {
                if (!result.TryAdd(remoteItem.RelativePath, remoteItem))
                {
                    throw new ArgumentException(
                        "Upload-only remote content contains duplicate relative paths.",
                        nameof(remoteContent));
                }
            }

            return result;
        }

        private static Dictionary<string, CottonUploadReceiptSnapshot> CreateReceiptSourceMap(
            IEnumerable<CottonUploadReceiptSnapshot> uploadReceipts)
        {
            var result = new Dictionary<string, CottonUploadReceiptSnapshot>(StringComparer.Ordinal);
            var operationIds = new HashSet<Guid>();
            foreach (CottonUploadReceiptSnapshot receipt in uploadReceipts)
            {
                if (!result.TryAdd(receipt.LocalSourceId, receipt))
                {
                    throw new ArgumentException(
                        "Upload receipt journal contains duplicate local source ids.",
                        nameof(uploadReceipts));
                }

                if (!operationIds.Add(receipt.OperationId))
                {
                    throw new ArgumentException(
                        "Upload receipt journal contains duplicate operation ids.",
                        nameof(uploadReceipts));
                }
            }

            return result;
        }

        private static Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> CreateRemoteOperationMap(
            CottonDeviceToCloudRemoteContentSnapshot remoteContent)
        {
            var result = new Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot>();
            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items)
            {
                if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File
                    || !remoteItem.Entry.Metadata.TryGetValue(
                        CottonFileUploadMetadataKeys.UploadOperationId,
                        out string? operationValue)
                    || !Guid.TryParseExact(operationValue, "N", out Guid operationId))
                {
                    continue;
                }

                if (!result.TryAdd(operationId, remoteItem))
                {
                    throw new ArgumentException(
                        "Remote content contains duplicate upload operation ids.",
                        nameof(remoteContent));
                }
            }

            return result;
        }
    }
}
