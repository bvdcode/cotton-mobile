// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncScanProgressReporter
    {
        private static readonly TimeSpan MinimumReportInterval = TimeSpan.FromMilliseconds(250);

        private readonly Guid _rootId;
        private readonly CottonSyncProgressHub _progressHub;
        private readonly TimeProvider _timeProvider;
        private long _lastReportTimestamp;
        private int _scannedItemCount;
        private int _reportedItemCount;

        public CottonSyncScanProgressReporter(
            Guid rootId,
            CottonSyncProgressHub progressHub,
            TimeProvider timeProvider)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            ArgumentNullException.ThrowIfNull(progressHub);
            ArgumentNullException.ThrowIfNull(timeProvider);

            _rootId = rootId;
            _progressHub = progressHub;
            _timeProvider = timeProvider;
            _lastReportTimestamp = timeProvider.GetTimestamp();
        }

        public void RecordScannedItem()
        {
            _scannedItemCount++;
            long timestamp = _timeProvider.GetTimestamp();
            if (_scannedItemCount > 1
                && _timeProvider.GetElapsedTime(_lastReportTimestamp, timestamp) < MinimumReportInterval)
            {
                return;
            }

            Report(timestamp);
        }

        public void Complete()
        {
            if (_reportedItemCount != _scannedItemCount)
            {
                Report(_timeProvider.GetTimestamp());
            }
        }

        private void Report(long timestamp)
        {
            _reportedItemCount = _scannedItemCount;
            _lastReportTimestamp = timestamp;
            _progressHub.Report(CottonSyncProgressSnapshot.ScanningDevice(
                _rootId,
                _scannedItemCount));
        }
    }
}
