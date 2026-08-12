// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserPresentation(
        string details,
        string actionLabel,
        string badgeText)
    {
        public string Details { get; } =
            details ?? throw new ArgumentNullException(nameof(details));

        public string ActionLabel { get; } =
            actionLabel ?? throw new ArgumentNullException(nameof(actionLabel));

        public string BadgeText { get; } = string.IsNullOrWhiteSpace(badgeText)
            ? CoreResources.FileBadge
            : badgeText.Trim();
    }
}
