// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidMediaStoreScanResult(
        CottonDeviceToCloudLocalContentSnapshot content,
        CottonContentRevisionIndexSnapshot? revisionIndex,
        AndroidMediaStoreScanStatistics statistics)
    {
        public CottonDeviceToCloudLocalContentSnapshot Content { get; } =
            content ?? throw new ArgumentNullException(nameof(content));

        public CottonContentRevisionIndexSnapshot? RevisionIndex { get; } = revisionIndex;

        public AndroidMediaStoreScanStatistics Statistics { get; } =
            statistics ?? throw new ArgumentNullException(nameof(statistics));
    }
}
#endif
