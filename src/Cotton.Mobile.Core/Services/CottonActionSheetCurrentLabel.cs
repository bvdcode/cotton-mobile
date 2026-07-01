// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonActionSheetCurrentLabel
    {
        public const string Suffix = " (current)";

        public static string Create(string label, bool isCurrent)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            return isCurrent ? label + Suffix : label;
        }

        public static string? Normalize(string? action)
        {
            if (action is null)
            {
                return null;
            }

            return action.EndsWith(Suffix, StringComparison.Ordinal)
                ? action[..^Suffix.Length]
                : action;
        }

        public static bool TryCreateDisplayLabel(string label, out string displayLabel)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            if (label.EndsWith(Suffix, StringComparison.Ordinal))
            {
                displayLabel = label[..^Suffix.Length];
                return true;
            }

            displayLabel = label;
            return false;
        }
    }
}
