// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonShareUploadNameValidator
    {
        public static bool TryNormalize(
            string? value,
            IEnumerable<string> existingNames,
            out string normalizedName,
            out string errorMessage)
        {
            ArgumentNullException.ThrowIfNull(existingNames);

            normalizedName = string.Empty;
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                errorMessage = "Enter a file name.";
                return false;
            }

            string name = value.Trim();
            if (CottonCloudItemNameRules.IsReservedPathSegment(name))
            {
                errorMessage = "Use a file name, not a folder path.";
                return false;
            }

            if (CottonCloudItemNameRules.ContainsInvalidCharacter(name))
            {
                errorMessage = "File names cannot contain path separators or reserved characters.";
                return false;
            }

            if (CottonCloudItemNameRules.ContainsDuplicateName(name, existingNames))
            {
                errorMessage = "Another captured file already uses that upload name.";
                return false;
            }

            normalizedName = name;
            return true;
        }
    }
}
