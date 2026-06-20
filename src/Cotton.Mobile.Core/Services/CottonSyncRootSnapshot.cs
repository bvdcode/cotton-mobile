using System.Security.Cryptography;
using System.Text;

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootSnapshot
    {
        public CottonSyncRootSnapshot(
            Guid id,
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot,
            CottonSyncDirection direction)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(id));
            }

            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!IsSupportedInstanceUri(instanceUri))
            {
                throw new ArgumentException("Sync root instance URI must be an absolute HTTPS URL.", nameof(instanceUri));
            }

            if (string.IsNullOrWhiteSpace(accountScopeKey))
            {
                throw new ArgumentException("Sync root account scope key is required.", nameof(accountScopeKey));
            }

            ArgumentNullException.ThrowIfNull(cloudFolder);
            ArgumentNullException.ThrowIfNull(localRoot);
            if (!Enum.IsDefined(direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported.");
            }

            Id = id;
            InstanceUri = NormalizeInstanceUri(instanceUri);
            AccountScopeKey = accountScopeKey.Trim();
            CloudFolder = cloudFolder;
            LocalRoot = localRoot;
            Direction = direction;
            StableKey = CreateStableKey(InstanceUri, AccountScopeKey, CloudFolder, LocalRoot);
            ReadinessStatus = ResolveReadinessStatus(LocalRoot.PermissionStatus);
            StatusText = CreateStatusText(ReadinessStatus);
        }

        public Guid Id { get; }

        public Uri InstanceUri { get; }

        public string AccountScopeKey { get; }

        public CottonUploadDestinationSnapshot CloudFolder { get; }

        public CottonSyncLocalRootSnapshot LocalRoot { get; }

        public CottonSyncDirection Direction { get; }

        public string StableKey { get; }

        public CottonSyncRootReadinessStatus ReadinessStatus { get; }

        public string StatusText { get; }

        public bool CanRunSync => ReadinessStatus == CottonSyncRootReadinessStatus.Ready;

        public bool IsBidirectional => Direction == CottonSyncDirection.Bidirectional;

        public bool NeedsUserAction => LocalRoot.NeedsUserAction;

        private static bool IsSupportedInstanceUri(Uri instanceUri)
        {
            return instanceUri.IsAbsoluteUri
                && string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(instanceUri.Host)
                && string.IsNullOrWhiteSpace(instanceUri.UserInfo)
                && string.IsNullOrWhiteSpace(instanceUri.Query)
                && string.IsNullOrWhiteSpace(instanceUri.Fragment);
        }

        private static Uri NormalizeInstanceUri(Uri instanceUri)
        {
            var builder = new UriBuilder(instanceUri)
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

        private static string CreateStableKey(
            Uri instanceUri,
            string accountScopeKey,
            CottonUploadDestinationSnapshot cloudFolder,
            CottonSyncLocalRootSnapshot localRoot)
        {
            string source = string.Join(
                "|",
                instanceUri.AbsoluteUri,
                accountScopeKey,
                cloudFolder.FolderId.ToString("N"),
                localRoot.StorageKind.ToString(),
                localRoot.RootKey);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
        }

        private static CottonSyncRootReadinessStatus ResolveReadinessStatus(
            CottonSyncRootPermissionStatus permissionStatus)
        {
            return permissionStatus switch
            {
                CottonSyncRootPermissionStatus.Available => CottonSyncRootReadinessStatus.Ready,
                CottonSyncRootPermissionStatus.NeedsUserGrant => CottonSyncRootReadinessStatus.NeedsUserGrant,
                CottonSyncRootPermissionStatus.Revoked => CottonSyncRootReadinessStatus.GrantRevoked,
                CottonSyncRootPermissionStatus.Unavailable => CottonSyncRootReadinessStatus.LocalRootUnavailable,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(permissionStatus),
                    "Sync root permission status is not supported."),
            };
        }

        private static string CreateStatusText(CottonSyncRootReadinessStatus status)
        {
            return status switch
            {
                CottonSyncRootReadinessStatus.Ready => "Sync root ready",
                CottonSyncRootReadinessStatus.NeedsUserGrant => "Choose local folder",
                CottonSyncRootReadinessStatus.GrantRevoked => "Reconnect local folder",
                CottonSyncRootReadinessStatus.LocalRootUnavailable => "Local folder unavailable",
                _ => throw new ArgumentOutOfRangeException(nameof(status), "Sync root status is not supported."),
            };
        }
    }
}
