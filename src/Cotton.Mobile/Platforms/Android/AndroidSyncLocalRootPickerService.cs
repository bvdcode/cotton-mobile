// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;
using Android.Provider;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidSyncLocalRootPickerService : ICottonSyncLocalRootPickerService
    {
        private static readonly ActivityFlags PersistableGrantFlags =
            ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;

        private static readonly ActivityFlags PickerIntentFlags =
            PersistableGrantFlags
            | ActivityFlags.GrantPersistableUriPermission
            | ActivityFlags.GrantPrefixUriPermission;

        private readonly IAndroidDocumentTreeActivityResultBridge _activityResultBridge;
        private readonly ICottonMediaAlbumPickerService _mediaAlbumPicker;
        private readonly IUserDialogService _dialogService;

        public AndroidSyncLocalRootPickerService(
            IAndroidDocumentTreeActivityResultBridge activityResultBridge,
            ICottonMediaAlbumPickerService mediaAlbumPicker,
            IUserDialogService dialogService)
        {
            ArgumentNullException.ThrowIfNull(activityResultBridge);
            ArgumentNullException.ThrowIfNull(mediaAlbumPicker);
            ArgumentNullException.ThrowIfNull(dialogService);

            _activityResultBridge = activityResultBridge;
            _mediaAlbumPicker = mediaAlbumPicker;
            _dialogService = dialogService;
        }

        public bool IsAvailable => true;

        public Task<CottonSyncLocalRootSnapshot?> PickAsync(
            CottonSyncRootStorageKind storageKind,
            CancellationToken cancellationToken = default)
        {
            return storageKind switch
            {
                CottonSyncRootStorageKind.UserSelectedDocumentTree => PickDocumentTreeAsync(cancellationToken),
                CottonSyncRootStorageKind.MediaStore => PickMediaStoreAsync(cancellationToken),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(storageKind),
                    storageKind,
                    "Sync root storage kind is not supported."),
            };
        }

        private async Task<CottonSyncLocalRootSnapshot?> PickDocumentTreeAsync(
            CancellationToken cancellationToken)
        {
            Activity activity = Platform.CurrentActivity
                ?? throw new InvalidOperationException("Folder picker needs an active Android activity.");
            ContentResolver contentResolver = activity.ContentResolver
                ?? throw new InvalidOperationException("Folder picker needs an active Android content resolver.");

            Intent intent = new(Intent.ActionOpenDocumentTree);
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

        private async Task<CottonSyncLocalRootSnapshot?> PickMediaStoreAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = await MainThread
                .InvokeOnMainThreadAsync(
                    () => Permissions.RequestAsync<CottonMediaReadPermissionRequest>())
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            AndroidMediaReadAccessSnapshot access = AndroidMediaReadAccessResolver.Resolve();
            if (!access.HasAccess)
            {
                await _dialogService
                    .ShowAlertAsync(
                        SyncRootSetupResources.MediaAccessRequiredTitle,
                        SyncRootSetupResources.MediaAccessRequiredMessage,
                        AppResources.OkText)
                    .ConfigureAwait(false);
                return null;
            }

            IReadOnlyList<CottonMediaAlbumSnapshot> albums = await AndroidMediaStoreAlbumProvider
                .LoadAsync(access, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<CottonMediaAlbumSnapshot>? selectedAlbums = await _mediaAlbumPicker
                .PickAsync(albums, cancellationToken)
                .ConfigureAwait(false);
            if (selectedAlbums is null || selectedAlbums.Count == 0)
            {
                return null;
            }

            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.MediaStore,
                AndroidMediaStoreRootKey.Value,
                CreateMediaDisplayName(selectedAlbums),
                CottonSyncRootPermissionStatus.Available,
                AndroidMediaStoreScopeKey.Create(selectedAlbums.Select(album => album.Id)));
        }

        private static string CreateMediaDisplayName(IReadOnlyList<CottonMediaAlbumSnapshot> albums)
        {
            return albums.Count == 1
                ? albums[0].DisplayName
                : SyncRootSetupResources.CreateMediaAlbumsDisplayName(albums.Count);
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
            return string.IsNullOrWhiteSpace(name) ? AppResources.SelectedFolder : name;
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
