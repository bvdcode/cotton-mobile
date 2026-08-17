// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncUploadProgressReporter : IProgress<long>
    {
        private static readonly TimeSpan MinimumReportInterval = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan MinimumSpeedInterval = TimeSpan.FromMilliseconds(500);

        private readonly Lock _gate = new();
        private readonly Guid _rootId;
        private readonly string _itemName;
        private readonly int _completedItemCount;
        private readonly int _totalItemCount;
        private readonly long? _totalBytes;
        private readonly CottonSyncProgressHub _progressHub;
        private readonly TimeProvider _timeProvider;
        private long _lastReportTimestamp;
        private long _lastTransferredBytes;
        private long? _transferStartedTimestamp;
        private bool _hasReported;

        public CottonSyncUploadProgressReporter(
            Guid rootId,
            string itemName,
            int completedItemCount,
            int totalItemCount,
            long? totalBytes,
            CottonSyncProgressHub progressHub,
            TimeProvider timeProvider)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(itemName);
            ArgumentOutOfRangeException.ThrowIfNegative(completedItemCount);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalItemCount);
            if (completedItemCount >= totalItemCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedItemCount),
                    "Completed item count must precede the current upload.");
            }

            if (totalBytes.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(totalBytes.Value);
            }

            ArgumentNullException.ThrowIfNull(progressHub);
            ArgumentNullException.ThrowIfNull(timeProvider);

            _rootId = rootId;
            _itemName = itemName.Trim();
            _completedItemCount = completedItemCount;
            _totalItemCount = totalItemCount;
            _totalBytes = totalBytes;
            _progressHub = progressHub;
            _timeProvider = timeProvider;
            _lastReportTimestamp = timeProvider.GetTimestamp();
        }

        public void Report(long value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            CottonSyncProgressSnapshot? snapshot;
            lock (_gate)
            {
                snapshot = CreateSnapshot(value);
            }

            if (snapshot is not null)
            {
                _progressHub.Report(snapshot);
            }
        }

        private CottonSyncProgressSnapshot? CreateSnapshot(long transferredBytes)
        {
            if (_hasReported && transferredBytes <= _lastTransferredBytes)
            {
                return null;
            }

            long timestamp = _timeProvider.GetTimestamp();
            if (transferredBytes > 0 && !_transferStartedTimestamp.HasValue)
            {
                _transferStartedTimestamp = timestamp;
            }

            bool isComplete = _totalBytes.HasValue && transferredBytes >= _totalBytes.Value;
            if (_hasReported
                && !isComplete
                && transferredBytes != 0
                && _timeProvider.GetElapsedTime(_lastReportTimestamp, timestamp) < MinimumReportInterval)
            {
                return null;
            }

            TimeSpan? elapsed = _transferStartedTimestamp.HasValue
                ? _timeProvider.GetElapsedTime(_transferStartedTimestamp.Value, timestamp)
                : null;
            double? bytesPerSecond = elapsed >= MinimumSpeedInterval && transferredBytes > 0
                ? transferredBytes / elapsed.Value.TotalSeconds
                : null;
            _lastTransferredBytes = transferredBytes;
            _lastReportTimestamp = timestamp;
            _hasReported = true;

            CottonSyncTransferSnapshot transfer = new(
                _itemName,
                transferredBytes,
                _totalBytes,
                bytesPerSecond);
            return CottonSyncProgressSnapshot.UploadingFile(
                _rootId,
                _completedItemCount,
                _totalItemCount,
                transfer);
        }
    }
}
