// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonChunkUploadProgress(
        IProgress<long> progress,
        long completedBytes) : IProgress<long>
    {
        private readonly IProgress<long> _progress = progress
            ?? throw new ArgumentNullException(nameof(progress));
        private readonly long _completedBytes = completedBytes;

        public void Report(long value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _progress.Report(checked(_completedBytes + value));
        }
    }
}
