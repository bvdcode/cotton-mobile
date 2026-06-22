// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.ViewModels
{
    public class DiagnosticsItemViewModel
    {
        public DiagnosticsItemViewModel(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Diagnostics item label is required.", nameof(label));
            }

            Label = label.Trim();
            Value = string.IsNullOrWhiteSpace(value) ? "Not available" : value.Trim();
        }

        public string Label { get; }

        public string Value { get; }
    }
}
