// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidDeviceToCloudLocalFileContentSourceRouter(
        AndroidDocumentTreeDeviceToCloudLocalFileContentSource documentTreeSource,
        AndroidMediaStoreDeviceToCloudLocalFileContentSource mediaStoreSource) :
        ICottonDeviceToCloudLocalFileContentSource
    {
        private readonly AndroidDocumentTreeDeviceToCloudLocalFileContentSource _documentTreeSource =
            documentTreeSource ?? throw new ArgumentNullException(nameof(documentTreeSource));
        private readonly AndroidMediaStoreDeviceToCloudLocalFileContentSource _mediaStoreSource =
            mediaStoreSource ?? throw new ArgumentNullException(nameof(mediaStoreSource));

        public Task<Stream> OpenReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            return root.LocalRoot.StorageKind switch
            {
                CottonSyncRootStorageKind.UserSelectedDocumentTree =>
                    _documentTreeSource.OpenReadAsync(instanceUri, root, item, cancellationToken),
                CottonSyncRootStorageKind.MediaStore =>
                    _mediaStoreSource.OpenReadAsync(instanceUri, root, item, cancellationToken),
                CottonSyncRootStorageKind.AppPrivateDirectory =>
                    throw new InvalidOperationException("App-private storage is not a device-to-cloud sync source."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(root),
                    root.LocalRoot.StorageKind,
                    "Device-to-cloud local storage kind is not supported."),
            };
        }
    }
}
#endif
