// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupUploadedMediaSnapshot
    {
        public CottonCameraBackupUploadedMediaSnapshot(
            CottonCameraBackupMediaIdentity identity,
            DateTime uploadedAtUtc,
            Guid? remoteFileId,
            string? remoteFileName)
        {
            ArgumentNullException.ThrowIfNull(identity);

            Identity = identity;
            UploadedAtUtc = NormalizeUtc(uploadedAtUtc);
            RemoteFileId = remoteFileId == Guid.Empty ? null : remoteFileId;
            RemoteFileName = string.IsNullOrWhiteSpace(remoteFileName) ? null : remoteFileName.Trim();
        }

        public CottonCameraBackupMediaIdentity Identity { get; }

        public DateTime UploadedAtUtc { get; }

        public Guid? RemoteFileId { get; }

        public string? RemoteFileName { get; }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
        }
    }
}
