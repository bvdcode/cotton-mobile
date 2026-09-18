// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using System.Resources;

namespace Cotton.Mobile.Resources.Localization
{
    public static class MediaLocationResources
    {
        private static readonly ResourceManager ResourceManagerInstance = new(typeof(MediaLocationResources));

        public static string Title => GetString(nameof(Title));

        public static string Explanation => GetString(nameof(Explanation));

        public static string DeniedWarning => GetString(nameof(DeniedWarning));

        public static string AllowText => GetString(nameof(AllowText));

        public static string SettingsText => GetString(nameof(SettingsText));

        private static string GetString(string name)
        {
            return ResourceManagerInstance.GetString(name, CultureInfo.CurrentUICulture)
                ?? throw new InvalidOperationException($"Media location resource '{name}' is missing.");
        }
    }
}
