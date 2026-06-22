// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonLogoutCacheCleanupDisplayState
    {
        private CottonLogoutCacheCleanupDisplayState(CottonLogoutCacheCleanupSettings settings)
        {
            Settings = settings;
            IsEnabled = settings.ClearCachedFilesOnLogout;
            StatusText = IsEnabled ? "On" : "Off";
            DetailText = IsEnabled
                ? "Remove cached previews, saved folder lists, and downloaded files when you log out."
                : "Keep local cached files on this device when you log out.";
        }

        public CottonLogoutCacheCleanupSettings Settings { get; }

        public string Title => "Clear cache on logout";

        public bool IsEnabled { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public static CottonLogoutCacheCleanupDisplayState Create(CottonLogoutCacheCleanupSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            return new CottonLogoutCacheCleanupDisplayState(settings);
        }
    }
}
