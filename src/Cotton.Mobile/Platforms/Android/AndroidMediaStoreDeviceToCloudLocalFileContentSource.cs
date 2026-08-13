// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidMediaStoreDeviceToCloudLocalFileContentSource :
        ICottonDeviceToCloudLocalFileContentSource
    {
        public Task<Stream> OpenReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedInput(instanceUri, root, item);
            cancellationToken.ThrowIfCancellationRequested();

            string sourceId = item.LocalSourceId
                ?? throw new InvalidOperationException("Android media upload item is missing its content URI.");
            AndroidUri contentUri = AndroidUri.Parse(sourceId)
                ?? throw new InvalidOperationException("Android media upload item has an invalid content URI.");
            ContentResolver resolver = global::Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
            Stream stream = resolver.OpenInputStream(contentUri)
                ?? throw new IOException("Could not open Android media content for upload.");
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(stream);
        }

        private static void EnsureSupportedInput(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(item);

            if (!Uri.Equals(instanceUri, root.InstanceUri))
            {
                throw new InvalidOperationException("Device-to-cloud sync instance does not match the sync root.");
            }

            if (!root.CanRunSync || !root.LocalRoot.UsesMediaStore)
            {
                throw new InvalidOperationException("Android media sync root is not ready.");
            }

            if (root.Direction != CottonSyncDirection.DeviceToCloud
                || !item.RequiresUpload
                || item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only Android media file uploads can open MediaStore content.");
            }
        }
    }
}
#endif
