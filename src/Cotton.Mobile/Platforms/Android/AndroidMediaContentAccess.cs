// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidMediaContentAccess
    {
        private readonly bool _hasLocationPermission = HasLocationPermission;

        public static bool HasLocationPermission => !OperatingSystem.IsAndroidVersionAtLeast(29)
            || global::Android.App.Application.Context.CheckSelfPermission(
                Manifest.Permission.AccessMediaLocation) == Permission.Granted;

        public string CreateRevisionSourceVersion(string sourceVersion)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceVersion);
            string representation = _hasLocationPermission ? "location-access-granted-v1" : "location-access-denied-v1";
            return $"{sourceVersion}:{representation}";
        }

        public void EnsureUnchanged()
        {
            if (_hasLocationPermission != HasLocationPermission)
            {
                throw new IOException("Media location permission changed during the scan.");
            }
        }

        public string ComputeContentHash(
            ContentResolver resolver,
            AndroidUri uri,
            CancellationToken cancellationToken)
        {
            EnsureUnchanged();
            using Stream content = OpenRead(resolver, uri);
            string contentHash = CottonContentHash.ComputeSha256(content, cancellationToken);
            EnsureUnchanged();
            return contentHash;
        }

        public static bool IsMedia(string? contentType)
        {
            return contentType is not null
                && (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                    || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase));
        }

        public static Stream OpenRead(ContentResolver resolver, AndroidUri uri)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(uri);
            if (OperatingSystem.IsAndroidVersionAtLeast(29)
                && HasLocationPermission
                && string.Equals(uri.Authority, MediaStore.Authority, StringComparison.Ordinal))
            {
                AndroidUri originalUri = MediaStore.SetRequireOriginal(uri)
                    ?? throw new IOException("Original media URI is unavailable.");
                return resolver.OpenInputStream(originalUri)
                    ?? throw new IOException("Original media content is unavailable.");
            }

            return resolver.OpenInputStream(uri)
                ?? throw new IOException("Local file content is unavailable.");
        }
    }
}
