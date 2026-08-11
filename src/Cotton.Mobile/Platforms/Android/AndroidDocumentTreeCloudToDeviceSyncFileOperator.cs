// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeCloudToDeviceSyncFileOperator :
        ICottonUserSelectedDocumentTreeCloudToDeviceSyncFileOperator
    {
        private readonly ICottonFileBrowserService _fileBrowserService;

        public AndroidDocumentTreeCloudToDeviceSyncFileOperator(ICottonFileBrowserService fileBrowserService)
        {
            ArgumentNullException.ThrowIfNull(fileBrowserService);

            _fileBrowserService = fileBrowserService;
        }

        public async Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            CottonCloudToDeviceSyncRootValidator.EnsureSupported(
                instanceUri,
                root,
                CottonSyncRootStorageKind.UserSelectedDocumentTree);
            CottonFileBrowserEntry file = CottonCloudToDeviceFileEntryFactory.Create(item);
            CottonFileDownloadResult download = await _fileBrowserService
                .DownloadAsync(instanceUri, file, progress: null, cancellationToken)
                .ConfigureAwait(false);
            await Task.Run(
                    () => CreateFileStore(root).Write(item, download.FilePath, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            CottonCloudToDeviceSyncRootValidator.EnsureSupported(
                instanceUri,
                root,
                CottonSyncRootStorageKind.UserSelectedDocumentTree);
            return Task.Run(
                () => CreateFileStore(root).Rename(item, cancellationToken),
                cancellationToken);
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            CottonCloudToDeviceSyncRootValidator.EnsureSupported(
                instanceUri,
                root,
                CottonSyncRootStorageKind.UserSelectedDocumentTree);
            return Task.Run(
                () => CreateFileStore(root).Remove(item, cancellationToken),
                cancellationToken);
        }

        private static AndroidDocumentTreeSyncFileStore CreateFileStore(CottonSyncRootSnapshot root)
        {
            ContentResolver resolver = Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
            AndroidUri? treeUri = AndroidUri.Parse(root.LocalRoot.RootKey);
            return new AndroidDocumentTreeSyncFileStore(
                resolver,
                treeUri ?? throw new InvalidOperationException("Document-tree sync root URI is invalid."));
        }
    }
}
#endif
