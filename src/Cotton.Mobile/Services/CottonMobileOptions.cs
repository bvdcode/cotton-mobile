// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMobileOptions
    {
        public CottonMobileOptions(
            string applicationName,
            Uri privacyPolicyUri,
            string supportEmail)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new ArgumentException("Application name is required.", nameof(applicationName));
            }

            if (string.IsNullOrWhiteSpace(supportEmail) || supportEmail.Contains(' ', StringComparison.Ordinal))
            {
                throw new ArgumentException("Support email is required.", nameof(supportEmail));
            }

            ArgumentNullException.ThrowIfNull(privacyPolicyUri);
            EnsureHttpsUri(privacyPolicyUri, nameof(privacyPolicyUri));

            ApplicationName = applicationName.Trim();
            PrivacyPolicyUri = privacyPolicyUri;
            SupportEmail = supportEmail.Trim();
        }

        public string ApplicationName { get; }

        public Uri PrivacyPolicyUri { get; }

        public string SupportEmail { get; }

        private static void EnsureHttpsUri(Uri uri, string parameterName)
        {
            if (!uri.IsAbsoluteUri
                || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(uri.Host))
            {
                throw new ArgumentException("URI must be an absolute HTTPS URI.", parameterName);
            }
        }
    }
}
