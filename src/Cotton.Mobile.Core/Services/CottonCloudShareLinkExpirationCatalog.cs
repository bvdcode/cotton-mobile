// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonCloudShareLinkExpirationCatalog
    {
        private static readonly IReadOnlyList<CottonCloudShareLinkExpirationOption> Options =
        [
            new("1 hour", 60, isDefault: false),
            new("1 day", CottonCloudShareLinkPolicy.DefaultExpireAfterMinutes, isDefault: true),
            new("7 days", 60 * 24 * 7, isDefault: false),
            new("30 days", 60 * 24 * 30, isDefault: false),
            new("1 year", CottonCloudShareLinkPolicy.MaxExpireAfterMinutes, isDefault: false),
        ];

        public static IReadOnlyList<CottonCloudShareLinkExpirationOption> CreateOptions()
        {
            return Options;
        }

        public static CottonCloudShareLinkExpirationOption? FindByLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return null;
            }

            return Options.FirstOrDefault(
                option => string.Equals(option.Label, label.Trim(), StringComparison.Ordinal));
        }
    }
}
