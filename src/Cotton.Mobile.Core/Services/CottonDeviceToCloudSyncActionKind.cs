// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonDeviceToCloudSyncActionKind
    {
        CreateRemoteFolder,
        UploadNewFile,
        UploadChangedFile,
        KeepExistingFile,
        KeepExistingFolder,
        DeleteRemoteFile,
        RemoveManifestOrphan,
        RemotePathConflict,
        RemoteRevisionChanged,
        RemoteTargetMissing,
        NeedsFreshServerRevision,
        BlockedLocalItemName,
    }
}
