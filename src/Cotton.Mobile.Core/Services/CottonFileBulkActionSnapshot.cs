// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileBulkActionSnapshot
    {
        public CottonFileBulkActionSnapshot(
            CottonFileBulkActionKind kind,
            string label,
            bool isEnabled,
            string disabledReason)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Bulk action kind is unknown.");
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Bulk action label is required.", nameof(label));
            }

            Kind = kind;
            Label = label.Trim();
            IsEnabled = isEnabled;
            DisabledReason = string.IsNullOrWhiteSpace(disabledReason) ? string.Empty : disabledReason.Trim();
        }

        public CottonFileBulkActionKind Kind { get; }

        public string Label { get; }

        public bool IsEnabled { get; }

        public string DisabledReason { get; }
    }
}
