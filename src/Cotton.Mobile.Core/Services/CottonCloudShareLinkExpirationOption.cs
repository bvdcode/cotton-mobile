// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudShareLinkExpirationOption
    {
        public CottonCloudShareLinkExpirationOption(
            string label,
            int expireAfterMinutes,
            bool isDefault)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Share link expiration label cannot be empty.", nameof(label));
            }

            CottonCloudShareLinkPolicy.EnsureValidExpireAfterMinutes(expireAfterMinutes);

            Label = label.Trim();
            ExpireAfterMinutes = expireAfterMinutes;
            IsDefault = isDefault;
        }

        public string Label { get; }

        public int ExpireAfterMinutes { get; }

        public bool IsDefault { get; }
    }
}
