// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonStoredUploadReceipt
    {
        public int SchemaVersion { get; set; }

        public string? SyncRootStableKey { get; set; }

        public string? LocalSourceId { get; set; }

        public string? RelativePath { get; set; }

        public DateTime LocalUpdatedAtUtc { get; set; }

        public long? SizeBytes { get; set; }

        public string? ContentType { get; set; }

        public Guid OperationId { get; set; }

        public CottonUploadReceiptStatus Status { get; set; }

        public DateTime RecordedAtUtc { get; set; }

        public Guid? RemoteFileId { get; set; }

        public string? RemoteETag { get; set; }

        public string? ContentHash { get; set; }
    }
}
