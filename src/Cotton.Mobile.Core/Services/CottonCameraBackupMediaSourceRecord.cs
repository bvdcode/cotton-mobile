// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupMediaSourceRecord
    {
        public CottonCameraBackupMediaSourceRecord(
            string? sourceId,
            string? displayName,
            string? contentType,
            long? sizeBytes,
            DateTime? lastModifiedUtc,
            DateTime? capturedAtUtc)
        {
            SourceId = sourceId;
            DisplayName = displayName;
            ContentType = contentType;
            SizeBytes = sizeBytes;
            LastModifiedUtc = lastModifiedUtc;
            CapturedAtUtc = capturedAtUtc;
        }

        public string? SourceId { get; }

        public string? DisplayName { get; }

        public string? ContentType { get; }

        public long? SizeBytes { get; }

        public DateTime? LastModifiedUtc { get; }

        public DateTime? CapturedAtUtc { get; }
    }
}
