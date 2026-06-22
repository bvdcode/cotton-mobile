// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonTransferActivitySignal : ICottonTransferActivitySignal
    {
        public event EventHandler? TransferActivityChanged;

        public void NotifyTransferActivityChanged()
        {
            TransferActivityChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
