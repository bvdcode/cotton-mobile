// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupDestinationStorageEstimateDisplayState
    {
        private CottonCameraBackupDestinationStorageEstimateDisplayState(string summaryText)
        {
            SummaryText = summaryText;
        }

        public string SummaryText { get; }

        public static CottonCameraBackupDestinationStorageEstimateDisplayState Create(
            CottonCameraBackupSettings settings,
            CottonCameraBackupMediaAccessDisplayState mediaAccess,
            CottonCameraBackupDestinationStorageEstimate estimate,
            bool isCurrent)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(mediaAccess);
            ArgumentNullException.ThrowIfNull(estimate);

            if (!settings.HasDestination)
            {
                return Create(string.Empty);
            }

            if (!mediaAccess.CanScanFullLibrary)
            {
                return Create("Allow full media access to estimate backup storage.");
            }

            if (!isCurrent)
            {
                return Create("Save to refresh backup storage estimate.");
            }

            if (!estimate.HasPendingItems)
            {
                return Create("No new camera media needs backup storage.");
            }

            if (!estimate.HasUnknownSizes)
            {
                return Create(
                    $"{FormatCount(estimate.PendingCount, "new item", "new items")} · {FormatSize(estimate.KnownSizeBytes)} estimated upload storage.");
            }

            if (estimate.KnownSizeBytes > 0)
            {
                return Create(
                    $"{FormatCount(estimate.PendingCount, "new item", "new items")} · at least {FormatSize(estimate.KnownSizeBytes)} estimated upload storage; {FormatCount(estimate.UnknownSizeCount, "item has", "items have")} unknown size.");
            }

            return Create(
                $"{FormatCount(estimate.PendingCount, "new item", "new items")} · backup size unknown until the device reports it.");
        }

        private static CottonCameraBackupDestinationStorageEstimateDisplayState Create(string summaryText)
        {
            return new CottonCameraBackupDestinationStorageEstimateDisplayState(summaryText);
        }

        private static string FormatCount(int count, string singular, string plural)
        {
            return count == 1 ? $"1 {singular}" : $"{count:N0} {plural}";
        }

        private static string FormatSize(long sizeBytes)
        {
            return CottonFileSizeFormatter.Format(sizeBytes);
        }
    }
}
