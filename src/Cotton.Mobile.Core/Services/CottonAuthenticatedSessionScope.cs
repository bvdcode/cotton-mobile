// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAuthenticatedSessionScope
    {
        public CottonAuthenticatedSessionScope(Uri instanceUri, string accountScopeKey)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentException.ThrowIfNullOrWhiteSpace(accountScopeKey);

            InstanceUri = instanceUri;
            AccountScopeKey = accountScopeKey.Trim();
        }

        public Uri InstanceUri { get; }

        public string AccountScopeKey { get; }
    }
}
