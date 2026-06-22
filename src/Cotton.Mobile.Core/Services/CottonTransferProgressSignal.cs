// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTransferProgressSignal : ICottonTransferProgressSignal
    {
        public event EventHandler<CottonTransferProgressChangedEventArgs>? TransferProgressChanged;

        public void NotifyTransferProgressChanged(
            Guid transferId,
            CottonTransferProgressSnapshot progress)
        {
            TransferProgressChanged?.Invoke(
                this,
                new CottonTransferProgressChangedEventArgs(transferId, progress));
        }
    }
}
