// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncWorkingFileName
    {
        private const int UniqueIdentifierLength = 32;
        private const string TemporarySuffix = ".cotton-sync-tmp";
        private const string BackupSuffix = ".cotton-sync-backup";

        public static string CreateTemporary(string displayName)
        {
            return Create(displayName, TemporarySuffix);
        }

        public static string CreateBackup(string displayName)
        {
            return Create(displayName, BackupSuffix);
        }

        public static bool IsWorkingFile(string displayName)
        {
            return HasGeneratedNameFormat(displayName, TemporarySuffix)
                || HasGeneratedNameFormat(displayName, BackupSuffix);
        }

        private static string Create(string displayName, string suffix)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Document display name is required.", nameof(displayName));
            }

            return $"{displayName.Trim()}.{Guid.NewGuid():N}{suffix}";
        }

        private static bool HasGeneratedNameFormat(string displayName, string suffix)
        {
            if (string.IsNullOrEmpty(displayName)
                || !displayName.EndsWith(suffix, StringComparison.Ordinal))
            {
                return false;
            }

            int identifierStart = displayName.Length - suffix.Length - UniqueIdentifierLength;
            if (identifierStart < 2 || displayName[identifierStart - 1] != '.')
            {
                return false;
            }

            ReadOnlySpan<char> identifier = displayName.AsSpan(identifierStart, UniqueIdentifierLength);
            foreach (char character in identifier)
            {
                if (!Uri.IsHexDigit(character))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
