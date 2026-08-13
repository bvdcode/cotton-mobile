// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidMediaStoreScanResult(
        CottonDeviceToCloudLocalContentSnapshot content,
        CottonContentRevisionIndexSnapshot? revisionIndex)
    {
        public CottonDeviceToCloudLocalContentSnapshot Content { get; } =
            content ?? throw new ArgumentNullException(nameof(content));

        public CottonContentRevisionIndexSnapshot? RevisionIndex { get; } = revisionIndex;
    }
}
#endif
