// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidDeviceToCloudLocalTreeReaderRouter(
        AndroidDocumentTreeDeviceToCloudLocalTreeReader documentTreeReader,
        AndroidMediaStoreDeviceToCloudLocalTreeReader mediaStoreReader) :
        ICottonDeviceToCloudLocalTreeReader
    {
        private readonly AndroidDocumentTreeDeviceToCloudLocalTreeReader _documentTreeReader =
            documentTreeReader ?? throw new ArgumentNullException(nameof(documentTreeReader));
        private readonly AndroidMediaStoreDeviceToCloudLocalTreeReader _mediaStoreReader =
            mediaStoreReader ?? throw new ArgumentNullException(nameof(mediaStoreReader));

        public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            return root.LocalRoot.StorageKind switch
            {
                CottonSyncRootStorageKind.UserSelectedDocumentTree =>
                    _documentTreeReader.ReadAsync(instanceUri, root, cancellationToken),
                CottonSyncRootStorageKind.MediaStore =>
                    _mediaStoreReader.ReadAsync(instanceUri, root, cancellationToken),
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
