// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Controls
{
    internal static class MaterialResources
    {
        public static T Get<T>(string key)
        {
            ResourceDictionary? resources = Application.Current?.Resources;
            if (resources?.TryGetValue(key, out object value) == true && value is T typedValue)
            {
                return typedValue;
            }

            throw new InvalidOperationException($"Material resource '{key}' was not found.");
        }

        public static void SetThemeColor(
            BindableObject bindable,
            BindableProperty property,
            string lightResourceKey,
            string darkResourceKey)
        {
            bindable.SetAppThemeColor(
                property,
                Get<Color>(lightResourceKey),
                Get<Color>(darkResourceKey));
        }

        public static Color GetThemeColor(string lightResourceKey, string darkResourceKey)
        {
            string resourceKey = Application.Current?.RequestedTheme == AppTheme.Light
                ? lightResourceKey
                : darkResourceKey;

            return Get<Color>(resourceKey);
        }
    }
}
