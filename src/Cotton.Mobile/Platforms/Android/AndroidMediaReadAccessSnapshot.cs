// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidMediaReadAccessSnapshot(
        bool canReadImages,
        bool canReadVideos,
        bool hasSelectedAccess = false)
    {
        private readonly bool _hasSelectedAccess = hasSelectedAccess;

        public bool CanReadImages { get; } = canReadImages;

        public bool CanReadVideos { get; } = canReadVideos;

        public bool HasAccess => CanReadImages || CanReadVideos;

        public bool HasLimitedAccess => _hasSelectedAccess && !HasAccess;
    }
}
#endif
