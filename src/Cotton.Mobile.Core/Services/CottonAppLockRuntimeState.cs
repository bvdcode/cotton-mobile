// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAppLockRuntimeState
    {
        public CottonAppLockRuntimeState(
            DateTimeOffset? lastBackgroundedAtUtc,
            DateTimeOffset? lastUnlockedAtUtc)
        {
            LastBackgroundedAtUtc = Normalize(lastBackgroundedAtUtc);
            LastUnlockedAtUtc = Normalize(lastUnlockedAtUtc);
        }

        public DateTimeOffset? LastBackgroundedAtUtc { get; }

        public DateTimeOffset? LastUnlockedAtUtc { get; }

        public static CottonAppLockRuntimeState Empty { get; } = new(null, null);

        public CottonAppLockRuntimeState WithBackgroundedAt(DateTimeOffset backgroundedAtUtc)
        {
            return new CottonAppLockRuntimeState(backgroundedAtUtc, LastUnlockedAtUtc);
        }

        public CottonAppLockRuntimeState WithUnlockedAt(DateTimeOffset unlockedAtUtc)
        {
            return new CottonAppLockRuntimeState(LastBackgroundedAtUtc, unlockedAtUtc);
        }

        private static DateTimeOffset? Normalize(DateTimeOffset? value)
        {
            return value?.ToUniversalTime();
        }
    }
}
