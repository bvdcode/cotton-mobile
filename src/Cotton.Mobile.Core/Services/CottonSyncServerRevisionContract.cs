// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonSyncServerRevisionContract
    {
        public static CottonSyncServerRevisionSnapshot Create(CottonFileBrowserEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.Type != CottonFileBrowserEntryType.File)
            {
                return CottonSyncServerRevisionSnapshot.FolderUnsupported();
            }

            return string.IsNullOrWhiteSpace(entry.ETag)
                ? CottonSyncServerRevisionSnapshot.FileMissingETag()
                : CottonSyncServerRevisionSnapshot.FileWithETag(entry.ETag);
        }
    }
}
