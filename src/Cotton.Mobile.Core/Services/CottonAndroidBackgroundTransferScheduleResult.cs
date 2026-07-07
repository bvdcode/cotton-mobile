// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAndroidBackgroundTransferScheduleResult
    {
        private CottonAndroidBackgroundTransferScheduleResult(
            CottonAndroidBackgroundTransferScheduleStatus status,
            CottonAndroidBackgroundTransferRequest? request,
            string statusText)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Schedule status is not supported.");
            }

            if (string.IsNullOrWhiteSpace(statusText))
            {
                throw new ArgumentException("Schedule status text is required.", nameof(statusText));
            }

            Status = status;
            Request = request;
            StatusText = statusText.Trim();
        }

        public CottonAndroidBackgroundTransferScheduleStatus Status { get; }

        public CottonAndroidBackgroundTransferRequest? Request { get; }

        public string StatusText { get; }

        public bool IsScheduled => Status == CottonAndroidBackgroundTransferScheduleStatus.Scheduled;

        public static CottonAndroidBackgroundTransferScheduleResult Scheduled(
            CottonAndroidBackgroundTransferRequest request,
            string statusText)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new CottonAndroidBackgroundTransferScheduleResult(
                CottonAndroidBackgroundTransferScheduleStatus.Scheduled,
                request,
                statusText);
        }

        public static CottonAndroidBackgroundTransferScheduleResult ForegroundRequired(
            CottonAndroidBackgroundTransferRequest request,
            string statusText)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new CottonAndroidBackgroundTransferScheduleResult(
                CottonAndroidBackgroundTransferScheduleStatus.ForegroundRequired,
                request,
                statusText);
        }

        public static CottonAndroidBackgroundTransferScheduleResult Unsupported(
            CottonAndroidBackgroundTransferRequest request,
            string statusText)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new CottonAndroidBackgroundTransferScheduleResult(
                CottonAndroidBackgroundTransferScheduleStatus.Unsupported,
                request,
                statusText);
        }

        public static CottonAndroidBackgroundTransferScheduleResult NoQueuedTransfer()
        {
            return new CottonAndroidBackgroundTransferScheduleResult(
                CottonAndroidBackgroundTransferScheduleStatus.NoQueuedTransfer,
                request: null,
                "No waiting upload is ready for Android background transfer.");
        }
    }
}
