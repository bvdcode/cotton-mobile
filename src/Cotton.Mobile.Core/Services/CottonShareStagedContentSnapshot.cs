// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonShareStagedContentSnapshot
    {
        public CottonShareStagedContentSnapshot(
            Guid intakeId,
            Guid itemId,
            string fileName,
            string path,
            long sizeBytes)
        {
            if (intakeId == Guid.Empty)
            {
                throw new ArgumentException("Share intake id cannot be empty.", nameof(intakeId));
            }

            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Share intake item id cannot be empty.", nameof(itemId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Staged share file name cannot be empty.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Staged share path cannot be empty.", nameof(path));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes));
            }

            IntakeId = intakeId;
            ItemId = itemId;
            FileName = fileName.Trim();
            Path = path.Trim();
            SizeBytes = sizeBytes;
        }

        public Guid IntakeId { get; }

        public Guid ItemId { get; }

        public string FileName { get; }

        public string Path { get; }

        public long SizeBytes { get; }
    }
}
