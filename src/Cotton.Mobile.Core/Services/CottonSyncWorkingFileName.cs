// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncWorkingFileName
    {
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
            return displayName.EndsWith(TemporarySuffix, StringComparison.Ordinal)
                || displayName.EndsWith(BackupSuffix, StringComparison.Ordinal);
        }

        private static string Create(string displayName, string suffix)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Document display name is required.", nameof(displayName));
            }

            return $"{displayName.Trim()}.{Guid.NewGuid():N}{suffix}";
        }
    }
}
