// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class CottonSyncRootStoreMapper
    {
        public static CottonStoredSyncRootCollection CreateStoredCollection(
            IReadOnlyCollection<CottonSyncRootSnapshot> roots,
            int schemaVersion)
        {
            return new CottonStoredSyncRootCollection
            {
                SchemaVersion = schemaVersion,
                SavedAtUtc = DateTime.UtcNow,
                Items = Deduplicate(roots)
                    .Select(CreateStoredItem)
                    .ToList(),
            };
        }

        public static CottonSyncRootSnapshot? TryCreateSyncRoot(
            Uri expectedInstanceUri,
            CottonStoredSyncRootItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.InstanceUri)
                    || !Uri.TryCreate(item.InstanceUri, UriKind.Absolute, out Uri? storedInstanceUri))
                {
                    return null;
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
                if (!IsSameInstance(root.InstanceUri, expectedInstanceUri)
                    || string.IsNullOrWhiteSpace(item.StableKey)
                    || !string.Equals(root.StableKey, item.StableKey.Trim(), StringComparison.Ordinal))
                {
                    return null;
                }

                return root;
            }
            catch (Exception exception)
                when (exception is ArgumentException or ArgumentOutOfRangeException or UriFormatException)
            {
                return null;
            }
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
            return roots
                .GroupBy(root => root.Id)
                .Select(group => group.Last())
                .GroupBy(root => root.StableKey, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToList();
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
