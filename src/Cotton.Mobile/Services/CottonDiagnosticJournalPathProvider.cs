// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public static class CottonDiagnosticJournalPathProvider
    {
        private const string DirectoryName = "CottonDiagnostics";

        public static string CreateDirectoryPath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DirectoryName);
        }
    }
}
