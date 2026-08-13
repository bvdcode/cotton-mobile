// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonInstanceUri
    {
        public static void EnsureSupported(Uri instanceUri, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!IsSupported(instanceUri))
            {
                throw new ArgumentException("Cotton instance URL must be an absolute HTTP or HTTPS URL.", parameterName);
            }
        }

        public static bool IsSupported(Uri instanceUri)
        {
            return instanceUri.IsAbsoluteUri
                && IsHttpScheme(instanceUri)
                && !string.IsNullOrWhiteSpace(instanceUri.Host)
                && string.IsNullOrWhiteSpace(instanceUri.UserInfo)
                && string.IsNullOrWhiteSpace(instanceUri.Query)
                && string.IsNullOrWhiteSpace(instanceUri.Fragment);
        }

        private static bool IsHttpScheme(Uri instanceUri)
        {
            return string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(instanceUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        }
    }
}
