// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonShareTextUploadName
    {
        public const string DefaultTextUploadName = "Shared text.txt";
        public const string TextContentType = "text/plain";

        public static string Create(string? requestedName)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
            {
                return DefaultTextUploadName;
            }

            string name = requestedName.Trim();
            if (CottonCloudItemNameRules.IsReservedPathSegment(name)
                || CottonCloudItemNameRules.ContainsInvalidCharacter(name))
            {
                return DefaultTextUploadName;
            }

            return Path.HasExtension(name) ? name : $"{name}.txt";
        }
    }
}
