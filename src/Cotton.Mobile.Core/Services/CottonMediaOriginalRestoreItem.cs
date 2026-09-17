// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMediaOriginalRestoreItem
    {
        internal CottonMediaOriginalRestoreItem(
            CottonDeviceToCloudLocalItemSnapshot localFile,
            CottonFileBrowserEntry remoteFile)
        {
            LocalFile = localFile;
            RemoteFile = remoteFile;
        }

        public CottonDeviceToCloudLocalItemSnapshot LocalFile { get; }

        public CottonFileBrowserEntry RemoteFile { get; }

    }
}
