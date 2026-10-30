// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class CottonSyncRootStoreMapper
    {
        public static CottonStoredSyncRootCollection CreateStoredCollection(
            IReadOnlyCollection<CottonSyncRootSnapshot> roots,
            int schemaVersion,
            DateTime savedAt)
        {
            return new CottonStoredSyncRootCollection
            {
                SchemaVersion = schemaVersion,
                SavedAtUtc = savedAt,
                Items = [.. Deduplicate(roots).Select<CottonSyncRootSnapshot, CottonStoredSyncRootItem?>(CreateStoredItem)],
            };
        }

        public static CottonSyncRootSnapshot CreateSyncRoot(
            Uri expectedInstanceUri,
            CottonStoredSyncRootItem? item)
        {
            if (item is null)
            {
                throw new InvalidDataException("The sync-root metadata contains an empty item.");
            }

            if (string.IsNullOrWhiteSpace(item.InstanceUri)
                || !Uri.TryCreate(item.InstanceUri, UriKind.Absolute, out Uri? storedInstanceUri))
            {
                throw new InvalidDataException("The sync-root metadata contains an invalid instance URI.");
            }

            CottonSyncRootSnapshot root = new(
                item.Id,
                storedInstanceUri,
                item.AccountScopeKey ?? string.Empty,
                new CottonUploadDestinationSnapshot(
                    item.CloudFolderId,
                    item.CloudFolderName ?? string.Empty,
                    item.CloudFolderPath),
                new CottonSyncLocalRootSnapshot(
                    item.LocalStorageKind,
                    item.LocalRootKey ?? string.Empty,
                    item.LocalRootDisplayName ?? string.Empty,
                    item.LocalPermissionStatus),
                item.Direction,
                item.UploadOriginalRetention);
            if (!HasValidIdentity(expectedInstanceUri, item, root))
            {
                throw new InvalidDataException("The sync-root metadata identity is invalid.");
            }

            return root;
        }

        private static bool HasValidIdentity(
            Uri expectedInstanceUri,
            CottonStoredSyncRootItem item,
            CottonSyncRootSnapshot root)
        {
            return IsSameInstance(root.InstanceUri, expectedInstanceUri)
                && !string.IsNullOrWhiteSpace(item.StableKey)
                && string.Equals(root.StableKey, item.StableKey.Trim(), StringComparison.Ordinal);
        }

        public static void EnsureRootsMatchInstance(
            Uri instanceUri,
            IReadOnlyCollection<CottonSyncRootSnapshot> roots)
        {
            foreach (CottonSyncRootSnapshot root in roots)
            {
                if (!IsSameInstance(root.InstanceUri, instanceUri))
                {
                    throw new ArgumentException("Sync roots must match the metadata instance.", nameof(roots));
                }
            }
        }

        public static List<CottonSyncRootSnapshot> Deduplicate(
            IReadOnlyCollection<CottonSyncRootSnapshot> roots)
        {
            return [.. roots
                .GroupBy(root => root.Id)
                .Select(group => group.Last())
                .GroupBy(root => root.StableKey, StringComparer.Ordinal)
                .Select(group => group.Last())];
        }

        private static CottonStoredSyncRootItem CreateStoredItem(CottonSyncRootSnapshot root)
        {
            return new CottonStoredSyncRootItem
            {
                Id = root.Id,
                InstanceUri = root.InstanceUri.AbsoluteUri,
                AccountScopeKey = root.AccountScopeKey,
                CloudFolderId = root.CloudFolder.FolderId,
                CloudFolderName = root.CloudFolder.FolderName,
                CloudFolderPath = root.CloudFolder.Path,
                LocalStorageKind = root.LocalRoot.StorageKind,
                LocalRootKey = root.LocalRoot.RootKey,
                LocalRootDisplayName = root.LocalRoot.DisplayName,
                LocalPermissionStatus = root.LocalRoot.PermissionStatus,
                Direction = root.Direction,
                UploadOriginalRetention = root.UploadOriginalRetention,
                StableKey = root.StableKey,
            };
        }

        private static bool IsSameInstance(Uri first, Uri second)
        {
            return string.Equals(
                NormalizeInstanceUri(first).AbsoluteUri,
                NormalizeInstanceUri(second).AbsoluteUri,
                StringComparison.Ordinal);
        }

        private static Uri NormalizeInstanceUri(Uri instanceUri)
        {
            if (!instanceUri.IsAbsoluteUri
                || !string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(instanceUri.Host)
                || !string.IsNullOrWhiteSpace(instanceUri.UserInfo)
                || !string.IsNullOrWhiteSpace(instanceUri.Query)
                || !string.IsNullOrWhiteSpace(instanceUri.Fragment))
            {
                throw new ArgumentException(
                    "Sync root instance URI must be an absolute HTTPS URL.",
                    nameof(instanceUri));
            }

            UriBuilder builder = new(instanceUri)
            {
                Scheme = instanceUri.Scheme.ToLowerInvariant(),
                Host = instanceUri.Host.ToLowerInvariant(),
            };

            if (builder.Uri.IsDefaultPort)
            {
                builder.Port = -1;
            }

            string path = builder.Path.TrimEnd('/');
            builder.Path = string.IsNullOrWhiteSpace(path) ? "/" : path;
            return builder.Uri;
        }
    }
}
