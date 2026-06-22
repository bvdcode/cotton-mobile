// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonTransferProgressSignal
    {
        event EventHandler<CottonTransferProgressChangedEventArgs>? TransferProgressChanged;

        void NotifyTransferProgressChanged(
            Guid transferId,
            CottonTransferProgressSnapshot progress);
    }
}
