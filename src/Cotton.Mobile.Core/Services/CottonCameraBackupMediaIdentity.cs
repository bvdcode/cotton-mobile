// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupMediaIdentity : IEquatable<CottonCameraBackupMediaIdentity>
    {
        public CottonCameraBackupMediaIdentity(
            string sourceId,
            DateTime? lastModifiedUtc,
            long? sizeBytes)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Backup media source id is required.", nameof(sourceId));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Backup media size cannot be negative.");
            }

            SourceId = sourceId.Trim();
            LastModifiedUtc = NormalizeUtc(lastModifiedUtc);
            SizeBytes = sizeBytes;
        }

        public string SourceId { get; }

        public DateTime? LastModifiedUtc { get; }

        public long? SizeBytes { get; }

        public bool Equals(CottonCameraBackupMediaIdentity? other)
        {
            return other is not null
                && string.Equals(SourceId, other.SourceId, StringComparison.Ordinal)
                && LastModifiedUtc == other.LastModifiedUtc
                && SizeBytes == other.SizeBytes;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as CottonCameraBackupMediaIdentity);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StringComparer.Ordinal.GetHashCode(SourceId), LastModifiedUtc, SizeBytes);
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (value is null)
            {
                return null;
            }

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
        }
    }
}
