// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncConflictSnapshot(
        CottonDeviceToCloudLocalItemSnapshot localFile,
        Guid remoteFileId,
        CottonFileBrowserEntryType remoteType,
        long? remoteSizeBytes,
        string? remoteContentHash,
        string? remoteETag,
        DateTime remoteUpdatedAt,
        string remoteRelativePath)
    {
        public CottonDeviceToCloudLocalItemSnapshot LocalFile { get; } = localFile;
        public Guid RemoteFileId { get; } = remoteFileId;
        public CottonFileBrowserEntryType RemoteType { get; } = remoteType;
        public long? RemoteSizeBytes { get; } = remoteSizeBytes;
        public string? RemoteContentHash { get; } = remoteContentHash;
        public string? RemoteETag { get; } = remoteETag;
        public DateTime RemoteUpdatedAt { get; } = remoteUpdatedAt;
        public string RemoteRelativePath { get; } = remoteRelativePath;

        [System.Text.Json.Serialization.JsonIgnore]
        public bool CanReplace => RemoteType == CottonFileBrowserEntryType.File
            && RemoteFileId != Guid.Empty && !string.IsNullOrWhiteSpace(RemoteETag)
            && LocalFile.LocalSourceId is not null && LocalFile.ContentHash is not null
            && LocalFile.SizeBytes.HasValue && LocalFile.RelativePath == RemoteRelativePath;

        public bool Matches(CottonSyncConflictSnapshot other)
        {
            return RemoteFileId == other.RemoteFileId && RemoteType == other.RemoteType
                && RemoteRelativePath == other.RemoteRelativePath
                && RemoteETag == other.RemoteETag && RemoteContentHash == other.RemoteContentHash
                && RemoteSizeBytes == other.RemoteSizeBytes
                && MatchesLocal(other.LocalFile);
        }

        public bool MatchesLocal(CottonDeviceToCloudLocalItemSnapshot local)
        {
            return LocalFile.LocalSourceId == local.LocalSourceId
                && LocalFile.RelativePath == local.RelativePath
                && LocalFile.SizeBytes == local.SizeBytes
                && LocalFile.ContentHash == local.ContentHash;
        }

        internal CottonDeviceToCloudSyncPlanItem ToPlanItem()
        {
            return new CottonDeviceToCloudSyncPlanItem(
                CottonDeviceToCloudSyncActionKind.RemotePathConflict,
                CottonFileBrowserEntryType.File, LocalFile.DisplayName, LocalFile.RelativePath,
                RemoteFileId, RemoteETag, LocalFile.LocalUpdatedAtUtc, LocalFile.SizeBytes,
                LocalFile.ContentType, LocalFile.LocalSourceId, contentHash: LocalFile.ContentHash);
        }
    }
}
