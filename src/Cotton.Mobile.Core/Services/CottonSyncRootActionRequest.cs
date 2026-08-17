// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootActionRequest
    {
        public CottonSyncRootActionRequest(
            CottonSyncRootListItem item,
            CottonSyncRootAction action)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!Enum.IsDefined(action))
            {
                throw new ArgumentOutOfRangeException(nameof(action), "Sync-root action is not supported.");
            }

            Item = item;
            Action = action;
        }

        public CottonSyncRootListItem Item { get; }

        public CottonSyncRootAction Action { get; }
    }
}
