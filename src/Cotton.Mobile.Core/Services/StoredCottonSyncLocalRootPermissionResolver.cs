// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class StoredCottonSyncLocalRootPermissionResolver : ICottonSyncLocalRootPermissionResolver
    {
        public CottonSyncRootPermissionStatus Resolve(CottonSyncLocalRootSnapshot localRoot)
        {
            ArgumentNullException.ThrowIfNull(localRoot);
            return localRoot.PermissionStatus;
        }
    }
}
