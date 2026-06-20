#if ANDROID
using Android.App;
using Android.Content;
using Android.Provider;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeSyncLocalRootPickerService : ICottonSyncLocalRootPickerService
    {
        private const string DefaultDisplayName = "Selected folder";

        private static readonly ActivityFlags PersistableGrantFlags =
            ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;

        private static readonly ActivityFlags PickerIntentFlags =
            PersistableGrantFlags
            | ActivityFlags.GrantPersistableUriPermission
            | ActivityFlags.GrantPrefixUriPermission;

        private readonly IAndroidDocumentTreeActivityResultBridge _activityResultBridge;

        public AndroidDocumentTreeSyncLocalRootPickerService(
            IAndroidDocumentTreeActivityResultBridge activityResultBridge)
        {
            ArgumentNullException.ThrowIfNull(activityResultBridge);

            _activityResultBridge = activityResultBridge;
        }

        public bool IsAvailable => true;

        public async Task<CottonSyncLocalRootSnapshot?> PickUserSelectedDocumentTreeAsync(
            CancellationToken cancellationToken = default)
        {
            Activity activity = Platform.CurrentActivity
                ?? throw new InvalidOperationException("Folder picker needs an active Android activity.");
            ContentResolver contentResolver = activity.ContentResolver
                ?? throw new InvalidOperationException("Folder picker needs an active Android content resolver.");

            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(PickerIntentFlags);

            Intent? resultIntent = await MainThread.InvokeOnMainThreadAsync(() =>
                    _activityResultBridge.StartOpenDocumentTreeAsync(activity, intent, cancellationToken))
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (resultIntent is null || resultIntent.Data is not AndroidUri uri)
            {
                return null;
            }

            PersistGrant(contentResolver, resultIntent, uri);
            string rootKey = uri.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rootKey))
            {
                throw new InvalidOperationException("Folder picker returned an empty document tree URI.");
            }

            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                rootKey,
                CreateDisplayName(uri),
                CottonSyncRootPermissionStatus.Available);
        }

        private static void PersistGrant(ContentResolver contentResolver, Intent resultIntent, AndroidUri uri)
        {
            ActivityFlags grantedFlags = resultIntent.Flags & PersistableGrantFlags;
            if ((grantedFlags & PersistableGrantFlags) != PersistableGrantFlags)
            {
                throw new InvalidOperationException("Selected folder did not grant read and write access.");
            }

            contentResolver.TakePersistableUriPermission(uri, grantedFlags);
        }

        private static string CreateDisplayName(AndroidUri uri)
        {
            string name = NormalizeDisplayName(DocumentsContract.GetTreeDocumentId(uri));
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            name = NormalizeDisplayName(uri.LastPathSegment);
            return string.IsNullOrWhiteSpace(name) ? DefaultDisplayName : name;
        }

        private static string NormalizeDisplayName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string candidate = value.Trim();
            int colonIndex = candidate.LastIndexOf(':');
            if (colonIndex >= 0 && colonIndex < candidate.Length - 1)
            {
                candidate = candidate[(colonIndex + 1)..].Trim();
            }

            int slashIndex = candidate.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex < candidate.Length - 1)
            {
                candidate = candidate[(slashIndex + 1)..].Trim();
            }

            return candidate;
        }
    }
}
#endif
