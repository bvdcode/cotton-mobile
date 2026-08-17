// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonAutomaticSyncFailureText
    {
        public static string Create(CottonAutomaticSyncFailureKind failureKind)
        {
            return failureKind switch
            {
                CottonAutomaticSyncFailureKind.AuthenticationRequired =>
                    CoreResources.SyncFailureAuthenticationRequired,
                CottonAutomaticSyncFailureKind.NetworkUnavailable =>
                    CoreResources.SyncFailureNetworkUnavailable,
                CottonAutomaticSyncFailureKind.LocalAccessUnavailable =>
                    CoreResources.SyncFailureLocalAccessUnavailable,
                CottonAutomaticSyncFailureKind.SourceChanged =>
                    CoreResources.SyncFailureSourceChanged,
                CottonAutomaticSyncFailureKind.TimedOut =>
                    CoreResources.SyncFailureTimedOut,
                CottonAutomaticSyncFailureKind.ServerRejectedRequest =>
                    CoreResources.SyncFailureServerRejectedRequest,
                CottonAutomaticSyncFailureKind.LocalReadFailed =>
                    CoreResources.SyncFailureLocalReadFailed,
                CottonAutomaticSyncFailureKind.Unexpected =>
                    CoreResources.SyncFailureUnexpected,
                CottonAutomaticSyncFailureKind.None => throw new ArgumentOutOfRangeException(
                    nameof(failureKind),
                    failureKind,
                    "A successful sync does not have failure details."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(failureKind),
                    failureKind,
                    "Automatic sync failure kind is not supported."),
            };
        }
    }
}
