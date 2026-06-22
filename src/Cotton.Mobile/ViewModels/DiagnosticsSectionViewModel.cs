// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.ViewModels
{
    public class DiagnosticsSectionViewModel
    {
        public DiagnosticsSectionViewModel(string title, IEnumerable<DiagnosticsItemViewModel> items)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Diagnostics section title is required.", nameof(title));
            }

            ArgumentNullException.ThrowIfNull(items);

            Title = title.Trim();
            Items = items.ToArray();
        }

        public string Title { get; }

        public IReadOnlyList<DiagnosticsItemViewModel> Items { get; }
    }
}
