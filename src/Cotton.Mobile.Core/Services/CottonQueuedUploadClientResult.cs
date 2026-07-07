// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonQueuedUploadClientResult
    {
        public CottonQueuedUploadClientResult(Guid? remoteFileId, string? remoteFileName)
        {
            RemoteFileId = remoteFileId == Guid.Empty ? null : remoteFileId;
            RemoteFileName = string.IsNullOrWhiteSpace(remoteFileName) ? null : remoteFileName.Trim();
        }

        public Guid? RemoteFileId { get; }

        public string? RemoteFileName { get; }
    }
}
