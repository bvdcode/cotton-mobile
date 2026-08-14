// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncRootSetupResult
    {
        public SyncRootSetupResult(
            SyncRootSetupStatus status,
            string message,
            CottonSyncRootSnapshot? root)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            Status = status;
            Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
            Root = ValidateRoot(status, root);
        }

        public SyncRootSetupStatus Status { get; }

        public string Message { get; }

        public CottonSyncRootSnapshot? Root { get; }

        public bool DidChangeRoots => Status is SyncRootSetupStatus.Created or SyncRootSetupStatus.Updated;

        private static CottonSyncRootSnapshot? ValidateRoot(
            SyncRootSetupStatus status,
            CottonSyncRootSnapshot? root)
        {
            bool requiresRoot = status switch
            {
                SyncRootSetupStatus.Created => true,
                SyncRootSetupStatus.Updated => true,
                SyncRootSetupStatus.AlreadyConfigured => true,
                SyncRootSetupStatus.Cancelled => false,
                SyncRootSetupStatus.Unavailable => false,
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Sync setup status is not supported."),
            };
            if (requiresRoot && root is null)
            {
                throw new ArgumentNullException(nameof(root), "Completed sync setup requires a root.");
            }

            if (!requiresRoot && root is not null)
            {
                throw new ArgumentException("Incomplete sync setup cannot include a root.", nameof(root));
            }

            return root;
        }
    }
}
