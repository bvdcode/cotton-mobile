// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonAutomaticSyncRetryPolicy
    {
        public static bool IsRetryable(CottonAutomaticSyncFailureKind failureKind)
        {
            return failureKind switch
            {
                CottonAutomaticSyncFailureKind.NetworkUnavailable => true,
                CottonAutomaticSyncFailureKind.TimedOut => true,
                CottonAutomaticSyncFailureKind.ServerUnavailable => true,
                CottonAutomaticSyncFailureKind.LocalReadFailed => true,
                CottonAutomaticSyncFailureKind.AuthenticationRequired => false,
                CottonAutomaticSyncFailureKind.LocalAccessUnavailable => false,
                CottonAutomaticSyncFailureKind.SourceChanged => false,
                CottonAutomaticSyncFailureKind.ServerRejectedRequest => false,
                CottonAutomaticSyncFailureKind.ActionRequired => false,
                CottonAutomaticSyncFailureKind.Unexpected => false,
                CottonAutomaticSyncFailureKind.None => false,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(failureKind),
                    failureKind,
                    "Automatic sync failure kind is not supported."),
            };
        }
    }
}
