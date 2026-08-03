// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class SyncRootSetupResult
    {
        public SyncRootSetupResult(SyncRootSetupStatus status, string message)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            Status = status;
            Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        }

        public SyncRootSetupStatus Status { get; }

        public string Message { get; }

        public bool DidChangeRoots => Status is SyncRootSetupStatus.Created or SyncRootSetupStatus.Updated;
    }
}
