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

            if (summary.RootResults.Any(result => result.Plan?.Items.Any(
                item => item.Action == CottonDeviceToCloudSyncActionKind.UploadedLocalVersionChanged) == true))
            {
                return CottonAutomaticSyncFailureKind.UploadedFileChanged;
            }

            return CottonAutomaticSyncFailureKind.ActionRequired;
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
