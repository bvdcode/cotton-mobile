// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonOfflineFolderFreeSpaceWarning
    {
        public CottonOfflineFolderFreeSpaceWarning(
            CottonOfflineFolderFreeSpaceWarningKind kind,
            string title,
            string message)
        {
            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Free-space warning kind is not supported.");
            }

            Kind = kind;
            Title = string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("Warning title is required.", nameof(title))
                : title.Trim();
            Message = string.IsNullOrWhiteSpace(message)
                ? throw new ArgumentException("Warning message is required.", nameof(message))
                : message.Trim();
        }

        public CottonOfflineFolderFreeSpaceWarningKind Kind { get; }

        public string Title { get; }

        public string Message { get; }

        public bool ShouldWarn => Kind != CottonOfflineFolderFreeSpaceWarningKind.None;

        public static CottonOfflineFolderFreeSpaceWarning None { get; } = new(
            CottonOfflineFolderFreeSpaceWarningKind.None,
            "Free space checked",
            "There is enough free device space for this offline folder.");
    }
}
