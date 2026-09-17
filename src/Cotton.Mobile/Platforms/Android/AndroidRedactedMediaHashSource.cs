// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Content;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidRedactedMediaHashSource : ICottonRedactedMediaHashSource
    {
        public bool IsSupported => OperatingSystem.IsAndroidVersionAtLeast(31);

        public bool CanReadOriginals => AndroidMediaContentAccess.HasLocationPermission;

        public Task<string?> ComputeAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalItemSnapshot file,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Compute(root, file, cancellationToken), cancellationToken);
        }

        private string? Compute(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalItemSnapshot file,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!OperatingSystem.IsAndroidVersionAtLeast(31) || !CanReadOriginals)
            {
                throw new InvalidOperationException("Original media access requires Android 12 and media permissions.");
            }

            ContentResolver resolver = global::Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Content resolver is unavailable.");
            using AndroidUri source = root.LocalRoot.StorageKind switch
            {
                CottonSyncRootStorageKind.MediaStore => AndroidUri.Parse(file.LocalSourceId)
                    ?? throw new InvalidDataException("Media URI is unavailable."),
                CottonSyncRootStorageKind.UserSelectedDocumentTree => DocumentsContract.BuildDocumentUriUsingTree(
                    AndroidUri.Parse(root.LocalRoot.RootKey), file.LocalSourceId)
                    ?? throw new InvalidDataException("Document URI is unavailable."),
                CottonSyncRootStorageKind.AppPrivateDirectory =>
                    throw new InvalidOperationException("This local folder cannot provide original media."),
                _ => throw new ArgumentOutOfRangeException(nameof(root), "Unsupported local folder kind."),
            };
            using AndroidUri? media = root.LocalRoot.UsesMediaStore
                ? AndroidUri.Parse(source.ToString())
                : MediaStore.GetMediaUri(global::Android.App.Application.Context, source);
            if (media is null)
            {
                return null;
            }

            using AndroidUri? redacted = MediaStore.GetRedactedUri(resolver, media);
            if (redacted is null)
            {
                return null;
            }

            AndroidMediaContentAccess access = new();
            using Stream content = resolver.OpenInputStream(redacted)
                ?? throw new IOException("Redacted media content is unavailable.");
            string hash = CottonContentHash.ComputeSha256(content, cancellationToken);
            access.EnsureUnchanged();
            return hash;
        }
    }
}
