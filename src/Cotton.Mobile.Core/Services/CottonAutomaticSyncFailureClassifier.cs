// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Net;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public static class CottonAutomaticSyncFailureClassifier
    {
        public static CottonAutomaticSyncFailureKind ClassifyBlocked(CottonDeviceToCloudSyncRunSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);
            if (!summary.HasBlockedItems)
            {
                throw new ArgumentException("The run has no blocked items.", nameof(summary));
            }

            CottonDeviceToCloudSyncPlanSnapshot[] plans =
                [.. summary.RootResults.Select(result => result.Plan).OfType<CottonDeviceToCloudSyncPlanSnapshot>()];
            CottonDeviceToCloudSyncPlanItem[] blockedItems =
                [.. plans.SelectMany(plan => plan.Items).Where(item => item.IsBlocked)];
            bool hasUnclassifiedBlocker = summary.RootResults.Any(result =>
                result.ExecutionResult is not null
                && result.ExecutionResult.BlockedCount > (result.Plan?.BlockedCount ?? 0));
            if (hasUnclassifiedBlocker || blockedItems.Length == 0)
            {
                return CottonAutomaticSyncFailureKind.ActionRequired;
            }

            CottonDeviceToCloudSyncActionKind[] actions = [.. blockedItems.Select(item => item.Action).Distinct()];
            if (actions.Length != 1)
            {
                return CottonAutomaticSyncFailureKind.ActionRequired;
            }

            return actions[0] switch
            {
                CottonDeviceToCloudSyncActionKind.RemotePathConflict =>
                    CottonAutomaticSyncFailureKind.RemotePathConflict,
                CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision =>
                    CottonAutomaticSyncFailureKind.RemoteRevisionChanged,
                CottonDeviceToCloudSyncActionKind.BlockedLocalItemName =>
                    CottonAutomaticSyncFailureKind.InvalidLocalItemName,
                CottonDeviceToCloudSyncActionKind.BlockedLocalSource =>
                    CottonAutomaticSyncFailureKind.LocalSourceUnavailable,
                CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged =>
                    CottonAutomaticSyncFailureKind.PendingUploadChanged,
                CottonDeviceToCloudSyncActionKind.UploadedLocalVersionChanged =>
                    CottonAutomaticSyncFailureKind.UploadedFileChanged,
                CottonDeviceToCloudSyncActionKind.CreateRemoteFolder or
                CottonDeviceToCloudSyncActionKind.UploadNewFile or
                CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload or
                CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile or
                CottonDeviceToCloudSyncActionKind.KeepExistingFile or
                CottonDeviceToCloudSyncActionKind.KeepExistingFolder => throw new ArgumentOutOfRangeException(
                    nameof(summary),
                    actions[0],
                    "A non-blocking action cannot classify a blocked sync."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(summary),
                    actions[0],
                    "Device-to-cloud sync action is not supported."),
            };
        }

        public static CottonAutomaticSyncFailureKind Classify(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (exception is CottonApiException { StatusCode: HttpStatusCode statusCode })
            {
                return ClassifyApiStatus(statusCode);
            }

            if (exception is CottonApiException)
            {
                return CottonAutomaticSyncFailureKind.ServerRejectedRequest;
            }

            if (exception is HttpRequestException)
            {
                return CottonAutomaticSyncFailureKind.NetworkUnavailable;
            }

            if (exception is UnauthorizedAccessException)
            {
                return CottonAutomaticSyncFailureKind.LocalAccessUnavailable;
            }

            if (exception is InvalidDataException)
            {
                return CottonAutomaticSyncFailureKind.SourceChanged;
            }

            if (exception is IOException)
            {
                return CottonAutomaticSyncFailureKind.LocalReadFailed;
            }

            if (exception is TimeoutException or OperationCanceledException)
            {
                return CottonAutomaticSyncFailureKind.TimedOut;
            }

            return CottonAutomaticSyncFailureKind.Unexpected;
        }

        private static CottonAutomaticSyncFailureKind ClassifyApiStatus(HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.InsufficientStorage)
            {
                return CottonAutomaticSyncFailureKind.InsufficientStorage;
            }

            if (statusCode == HttpStatusCode.NotFound)
            {
                return CottonAutomaticSyncFailureKind.RemoteContentUnavailable;
            }

            if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return CottonAutomaticSyncFailureKind.AuthenticationRequired;
            }

            if (statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout)
            {
                return CottonAutomaticSyncFailureKind.TimedOut;
            }

            if (statusCode == HttpStatusCode.TooManyRequests || (int)statusCode >= 500)
            {
                return CottonAutomaticSyncFailureKind.ServerUnavailable;
            }

            return CottonAutomaticSyncFailureKind.ServerRejectedRequest;
        }
    }
}
