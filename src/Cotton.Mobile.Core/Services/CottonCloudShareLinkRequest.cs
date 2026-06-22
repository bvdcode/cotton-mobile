// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCloudShareLinkRequest
    {
        private CottonCloudShareLinkRequest(
            CottonCloudShareLinkTargetKind targetKind,
            Guid targetId,
            int expireAfterMinutes)
        {
            if (!Enum.IsDefined(targetKind))
            {
                throw new ArgumentOutOfRangeException(nameof(targetKind));
            }

            if (targetId == Guid.Empty)
            {
                throw new ArgumentException("Share link target id is required.", nameof(targetId));
            }

            CottonCloudShareLinkPolicy.EnsureValidExpireAfterMinutes(expireAfterMinutes);

            TargetKind = targetKind;
            TargetId = targetId;
            ExpireAfterMinutes = expireAfterMinutes;
        }

        public CottonCloudShareLinkTargetKind TargetKind { get; }

        public Guid TargetId { get; }

        public int ExpireAfterMinutes { get; }

        public static CottonCloudShareLinkRequest ForFile(
            Guid fileId,
            int expireAfterMinutes = CottonCloudShareLinkPolicy.DefaultExpireAfterMinutes)
        {
            return new CottonCloudShareLinkRequest(
                CottonCloudShareLinkTargetKind.File,
                fileId,
                expireAfterMinutes);
        }

        public static CottonCloudShareLinkRequest ForFolder(
            Guid folderId,
            int expireAfterMinutes = CottonCloudShareLinkPolicy.DefaultExpireAfterMinutes)
        {
            return new CottonCloudShareLinkRequest(
                CottonCloudShareLinkTargetKind.Folder,
                folderId,
                expireAfterMinutes);
        }
    }
}
