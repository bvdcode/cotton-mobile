// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidDeviceToCloudLocalFileOperatorRouter(
        AndroidDocumentTreeDeviceToCloudLocalFileOperator documentTreeOperator) :
        ICottonDeviceToCloudLocalFileOperator
    {
        private readonly AndroidDocumentTreeDeviceToCloudLocalFileOperator _documentTreeOperator =
            documentTreeOperator ?? throw new ArgumentNullException(nameof(documentTreeOperator));

        public Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteIfUnchangedAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(root);
            return root.LocalRoot.StorageKind switch
            {
                CottonSyncRootStorageKind.UserSelectedDocumentTree =>
                    _documentTreeOperator.DeleteIfUnchangedAsync(instanceUri, root, item, cancellationToken),
                CottonSyncRootStorageKind.MediaStore =>
                    throw new InvalidOperationException("MediaStore sync roots must retain original media."),
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
