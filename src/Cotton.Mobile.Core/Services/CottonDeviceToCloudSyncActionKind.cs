// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonDeviceToCloudSyncActionKind
    {
        CreateRemoteFolder,
        UploadNewFile,
        ConfirmPendingUpload,
        DeleteUploadedLocalFile,
        KeepExistingFile,
        KeepExistingFolder,
        RemotePathConflict,
        NeedsFreshServerRevision,
        BlockedLocalItemName,
        BlockedLocalSource,
        PendingLocalVersionChanged,
    }
}
