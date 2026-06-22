// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTrashEmptyActionSnapshot
    {
        private CottonTrashEmptyActionSnapshot(
            int itemCount,
            bool isBusy,
            bool isSelectionModeActive)
        {
            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), "Trash item count cannot be negative.");
            }

            ItemCount = itemCount;
            IsBusy = isBusy;
            IsSelectionModeActive = isSelectionModeActive;
            Label = CottonTrashEmptyStatusText.ConfirmAction;
            DisabledReason = CreateDisabledReason();
            IsEnabled = string.IsNullOrWhiteSpace(DisabledReason);
            ConfirmationMessage = IsEnabled
                ? CottonTrashEmptyStatusText.CreateConfirmMessage(ItemCount)
                : string.Empty;
        }

        public int ItemCount { get; }

        public bool IsBusy { get; }

        public bool IsSelectionModeActive { get; }

        public string Label { get; }

        public bool IsEnabled { get; }

        public string DisabledReason { get; }

        public string ConfirmationMessage { get; }

        public static CottonTrashEmptyActionSnapshot Create(
            int itemCount,
            bool isBusy,
            bool isSelectionModeActive)
        {
            return new CottonTrashEmptyActionSnapshot(itemCount, isBusy, isSelectionModeActive);
        }

        private string CreateDisabledReason()
        {
            if (IsBusy)
            {
                return "Trash is busy.";
            }

            if (IsSelectionModeActive)
            {
                return "Finish selecting trash items first.";
            }

            return ItemCount == 0 ? "Trash is empty." : string.Empty;
        }
    }
}
