// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonCameraBackupMediaSourceRecordMapper
    {
        public static bool TryCreateCandidate(
            CottonCameraBackupMediaSourceRecord record,
            out CottonCameraBackupCandidate? candidate)
        {
            ArgumentNullException.ThrowIfNull(record);

            candidate = null;
            CottonCameraBackupMediaKind? kind = TryGetKind(record.ContentType);
            if (kind is null)
            {
                return false;
            }

            try
            {
                candidate = new CottonCameraBackupCandidate(
                    new CottonCameraBackupMediaIdentity(
                        record.SourceId ?? string.Empty,
                        record.LastModifiedUtc,
                        record.SizeBytes),
                    kind.Value,
                    record.DisplayName ?? string.Empty,
                    record.ContentType,
                    record.CapturedAtUtc);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static CottonCameraBackupMediaKind? TryGetKind(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return null;
            }

            string value = contentType.Trim();
            if (value.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return CottonCameraBackupMediaKind.Photo;
            }

            if (value.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return CottonCameraBackupMediaKind.Video;
            }

            return null;
        }
    }
}
