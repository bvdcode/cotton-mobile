// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal static class CottonInstanceUri
    {
        public static void EnsureSupported(Uri instanceUri, string parameterName)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!IsSupported(instanceUri))
            {
                throw new ArgumentException("Cotton instance URL must be an absolute HTTPS URL.", parameterName);
            }
        }

        public static bool IsSupported(Uri instanceUri)
        {
            return instanceUri.IsAbsoluteUri
                && string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(instanceUri.Host)
                && string.IsNullOrWhiteSpace(instanceUri.UserInfo)
                && string.IsNullOrWhiteSpace(instanceUri.Query)
                && string.IsNullOrWhiteSpace(instanceUri.Fragment);
        }
    }
}
