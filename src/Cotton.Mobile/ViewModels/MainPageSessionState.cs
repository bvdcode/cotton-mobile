// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.ViewModels
{
    public class MainPageSessionState
    {
        private MainPageSessionState(
            string instanceUrl,
            Uri? instanceUri,
            MainPageProfile? profile,
            string? status,
            bool reloadSync)
        {
            InstanceUrl = instanceUrl;
            InstanceUri = instanceUri;
            Profile = profile;
            Status = status;
            ReloadSync = reloadSync;
        }

        public string InstanceUrl { get; }

        public Uri? InstanceUri { get; }

        public Uri InstanceUriValue => InstanceUri
            ?? throw new InvalidOperationException("Authenticated session requires an instance URI.");

        public MainPageProfile? Profile { get; }

        public string? Status { get; }

        public bool ReloadSync { get; }

        public bool IsAuthenticated => InstanceUri is not null && Profile is not null;

        public static MainPageSessionState Authenticated(
            Uri instanceUri,
            MainPageProfile profile,
            string? status,
            bool reloadSync = true)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(profile);
            return new MainPageSessionState(instanceUri.AbsoluteUri, instanceUri, profile, status, reloadSync);
        }

        public static MainPageSessionState SignedOut(string instanceUrl, string? status)
        {
            ArgumentNullException.ThrowIfNull(instanceUrl);
            return new MainPageSessionState(instanceUrl, null, null, status, reloadSync: false);
        }
    }
}
