// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAppSwitcherPrivacyPolicy
    {
        public bool ShouldHidePreviews(
            CottonAppLockSettings settings,
            CottonAppLockCapabilitySnapshot capability)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(capability);

            return settings.IsEnabled && capability.CanEnable;
        }
    }
}
