// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.IO;

namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupCandidate
    {
        private const string DefaultPhotoContentType = "image/jpeg";
        private const string DefaultVideoContentType = "video/mp4";

        public CottonCameraBackupCandidate(
            CottonCameraBackupMediaIdentity identity,
            CottonCameraBackupMediaKind kind,
            string displayName,
            string? contentType,
            DateTime? capturedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(identity);

            Identity = identity;
            Kind = kind;
            DisplayName = NormalizeDisplayName(displayName, kind);
            ContentType = NormalizeContentType(contentType, kind);
            CapturedAtUtc = NormalizeUtc(capturedAtUtc);
        }

        public CottonCameraBackupMediaIdentity Identity { get; }

        public CottonCameraBackupMediaKind Kind { get; }

        public string DisplayName { get; }

        public string ContentType { get; }

        public DateTime? CapturedAtUtc { get; }

        public bool IsPhoto => Kind == CottonCameraBackupMediaKind.Photo;

        public bool IsVideo => Kind == CottonCameraBackupMediaKind.Video;

        private static string NormalizeDisplayName(string displayName, CottonCameraBackupMediaKind kind)
        {
            string value = string.IsNullOrWhiteSpace(displayName)
                ? kind == CottonCameraBackupMediaKind.Photo ? "photo.jpg" : "video.mp4"
                : Path.GetFileName(displayName.Trim());

            return string.IsNullOrWhiteSpace(value)
                ? kind == CottonCameraBackupMediaKind.Photo ? "photo.jpg" : "video.mp4"
                : value;
        }

        private static string NormalizeContentType(string? contentType, CottonCameraBackupMediaKind kind)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                return contentType.Trim();
            }

            return kind == CottonCameraBackupMediaKind.Photo
                ? DefaultPhotoContentType
                : DefaultVideoContentType;
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
