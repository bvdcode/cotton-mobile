// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using System.Resources;

namespace Cotton.Mobile.Resources.Localization
{
    public static class MediaRestoreResources
    {
        private static readonly ResourceManager ResourceManagerInstance = new(typeof(MediaRestoreResources));

        public static string Title => GetString(nameof(Title));
        public static string ConfirmationFormat => GetString(nameof(ConfirmationFormat));
        public static string Restore => GetString(nameof(Restore));
        public static string KeepCopies => GetString(nameof(KeepCopies));

        private static string GetString(string name)
        {
            return ResourceManagerInstance.GetString(name, CultureInfo.CurrentUICulture)
                ?? throw new InvalidOperationException($"Media restore resource '{name}' is missing.");
        }
    }
}
