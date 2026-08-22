// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncIgnoredFileName
    {
        private const string TemporaryFilePrefix = ".temp";

        public static bool IsIgnored(string fileName)
        {
            return !string.IsNullOrEmpty(fileName)
                && (fileName.StartsWith(TemporaryFilePrefix, StringComparison.OrdinalIgnoreCase)
                    || CottonSyncWorkingFileName.IsWorkingFile(fileName));
        }
    }
}
