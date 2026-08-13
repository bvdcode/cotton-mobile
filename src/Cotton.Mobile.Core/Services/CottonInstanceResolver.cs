// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonInstanceResolver(ICottonInstanceProbe probe) : ICottonInstanceResolver
    {
        private readonly ICottonInstanceProbe _probe = probe
            ?? throw new ArgumentNullException(nameof(probe));

        public async Task<Uri?> ResolveAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            string normalizedAddress = address.Trim();
            if (normalizedAddress.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal))
            {
                return NormalizeSupported(normalizedAddress);
            }

            Uri? httpsCandidate = CreateCandidate(Uri.UriSchemeHttps, normalizedAddress);
            if (httpsCandidate is not null
                && await _probe.IsCottonInstanceAsync(httpsCandidate, cancellationToken).ConfigureAwait(false))
            {
                return httpsCandidate;
            }

            Uri? httpCandidate = CreateCandidate(Uri.UriSchemeHttp, normalizedAddress);
            if (httpCandidate is not null
                && await _probe.IsCottonInstanceAsync(httpCandidate, cancellationToken).ConfigureAwait(false))
            {
                return httpCandidate;
            }

            return null;
        }

        private static Uri? CreateCandidate(string scheme, string address)
        {
            return NormalizeSupported($"{scheme}{Uri.SchemeDelimiter}{address}");
        }

        private static Uri? NormalizeSupported(string address)
        {
            Uri? instanceUri = CottonServerUrl.NormalizeOptional(address);
            return instanceUri is not null && CottonInstanceUri.IsSupported(instanceUri)
                ? instanceUri
                : null;
        }
    }
}
