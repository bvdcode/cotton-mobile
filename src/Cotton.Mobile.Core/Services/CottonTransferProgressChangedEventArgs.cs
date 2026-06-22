// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTransferProgressChangedEventArgs : EventArgs
    {
        public CottonTransferProgressChangedEventArgs(
            Guid transferId,
            CottonTransferProgressSnapshot progress)
        {
            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            ArgumentNullException.ThrowIfNull(progress);

            TransferId = transferId;
            Progress = progress;
        }

        public Guid TransferId { get; }

        public CottonTransferProgressSnapshot Progress { get; }
    }
}
