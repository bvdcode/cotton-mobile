// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Controls
{
    internal static class MaterialMotion
    {
        public static double Value(string resourceKey)
        {
            return MaterialResources.Get<double>(resourceKey);
        }

        public static uint Duration(string resourceKey)
        {
            return Duration(MaterialResources.Get<int>(resourceKey));
        }

        public static uint Duration(int duration)
        {
            return Convert.ToUInt32(duration, CultureInfo.InvariantCulture);
        }
    }
}
