// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public static class CottonAutomaticSyncStatusText
    {
        public static string Create(CottonAutomaticSyncRootStatusSnapshot status)
        {
            ArgumentNullException.ThrowIfNull(status);
            DateTime completedAt = status.CompletedAtUtc.ToLocalTime();
            return status.Outcome switch
            {
                CottonAutomaticSyncOutcome.Succeeded =>
                    CoreResources.Format(CoreResources.LastSyncSucceededFormat, completedAt),
                CottonAutomaticSyncOutcome.Failed =>
                    CreateFailureStatus(status.FailureKind, completedAt),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status.Outcome,
                    "Automatic sync outcome is not supported."),
            };
        }

        private static string CreateFailureStatus(
            CottonAutomaticSyncFailureKind failureKind,
            DateTime completedAt)
        {
            return failureKind switch
            {
                CottonAutomaticSyncFailureKind.ActionRequired or
                CottonAutomaticSyncFailureKind.UploadedFileChanged =>
                    CoreResources.Format(CoreResources.LastSyncNeedsReviewFormat, completedAt),
                CottonAutomaticSyncFailureKind.AuthenticationRequired or
                CottonAutomaticSyncFailureKind.NetworkUnavailable or
                CottonAutomaticSyncFailureKind.LocalAccessUnavailable or
                CottonAutomaticSyncFailureKind.SourceChanged or
                CottonAutomaticSyncFailureKind.TimedOut or
                CottonAutomaticSyncFailureKind.ServerUnavailable or
                CottonAutomaticSyncFailureKind.ServerRejectedRequest or
                CottonAutomaticSyncFailureKind.LocalReadFailed or
                CottonAutomaticSyncFailureKind.Unexpected or
                CottonAutomaticSyncFailureKind.RemoteContentUnavailable or
                CottonAutomaticSyncFailureKind.InsufficientStorage =>
                    CoreResources.Format(CoreResources.LastSyncFailedFormat, completedAt),
                CottonAutomaticSyncFailureKind.None => throw new ArgumentOutOfRangeException(
                    nameof(failureKind),
                    failureKind,
                    "A failed automatic sync must have a failure kind."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(failureKind),
                    failureKind,
                    "Automatic sync failure kind is not supported."),
            };
        }
    }
}
