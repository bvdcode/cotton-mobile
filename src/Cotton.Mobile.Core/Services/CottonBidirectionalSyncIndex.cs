// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonBidirectionalSyncIndex
    {
        public CottonBidirectionalSyncIndex(
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            IEnumerable<CottonSyncedFileSnapshot> manifestFiles)
        {
            LocalByPath = CreateLocalItemMap(localContent);
            RemoteByPath = CreateRemotePathMap(remoteContent);
            RemoteById = CreateRemoteIdMap(remoteContent);
            ManifestByPath = CreateManifestPathMap(manifestFiles);
        }

        public IReadOnlyDictionary<string, CottonDeviceToCloudLocalItemSnapshot> LocalByPath { get; }

        public IReadOnlyDictionary<string, CottonDeviceToCloudRemoteItemSnapshot> RemoteByPath { get; }

        public IReadOnlyDictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> RemoteById { get; }

        public IReadOnlyDictionary<string, CottonSyncedFileSnapshot> ManifestByPath { get; }

        public static int GetPathDepth(string relativePath)
        {
            return relativePath.Count(character => character == '/');
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
                        "Bidirectional local content contains duplicate relative paths.",
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
                        "Bidirectional remote content contains duplicate relative paths.",
                        nameof(remoteContent));
                }
            }

            return result;
        }

        private static Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> CreateRemoteIdMap(
            CottonDeviceToCloudRemoteContentSnapshot remoteContent)
        {
            var result = new Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot>();
            foreach (CottonDeviceToCloudRemoteItemSnapshot remoteItem in remoteContent.Items)
            {
                if (remoteItem.Entry.Type != CottonFileBrowserEntryType.File)
                {
                    continue;
                }

                if (!result.TryAdd(remoteItem.Entry.Id, remoteItem))
                {
                    throw new ArgumentException(
                        "Bidirectional remote content contains duplicate file ids.",
                        nameof(remoteContent));
                }
            }

            return result;
        }

        private static Dictionary<string, CottonSyncedFileSnapshot> CreateManifestPathMap(
            IEnumerable<CottonSyncedFileSnapshot> manifestFiles)
        {
            var result = new Dictionary<string, CottonSyncedFileSnapshot>(StringComparer.OrdinalIgnoreCase);
            var fileIds = new HashSet<Guid>();
            foreach (CottonSyncedFileSnapshot manifestFile in manifestFiles)
            {
                if (!fileIds.Add(manifestFile.FileId))
                {
                    throw new ArgumentException(
                        "Bidirectional manifest contains duplicate file ids.",
                        nameof(manifestFiles));
                }

                if (!result.TryAdd(manifestFile.RelativePath, manifestFile))
                {
                    throw new ArgumentException(
                        "Bidirectional manifest contains duplicate relative paths.",
                        nameof(manifestFiles));
                }
            }

            return result;
        }
    }
}
