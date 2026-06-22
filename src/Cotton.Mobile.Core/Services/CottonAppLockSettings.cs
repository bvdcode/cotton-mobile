// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAppLockSettings
    {
        public CottonAppLockSettings(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        public static CottonAppLockSettings Disabled { get; } = new(false);

        public bool IsEnabled { get; }

        public CottonAppLockSettings WithEnabled(bool isEnabled)
        {
            return IsEnabled == isEnabled ? this : new CottonAppLockSettings(isEnabled);
        }
    }
}
