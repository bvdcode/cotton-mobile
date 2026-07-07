// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileOpenRoute
    {
        internal CottonFileOpenRoute(
            CottonFileOpenTarget target,
            CottonFilePreviewKind previewKind,
            CottonSystemFileOpenKind systemKind,
            string actionLabel,
            string unavailableStatus,
            string? contentType)
        {
            if (target == CottonFileOpenTarget.InAppPreview && previewKind == CottonFilePreviewKind.None)
            {
                throw new ArgumentException("In-app file routes require a preview kind.", nameof(previewKind));
            }

            if (target == CottonFileOpenTarget.SystemApp && systemKind == CottonSystemFileOpenKind.None)
            {
                throw new ArgumentException("System file routes require a system kind.", nameof(systemKind));
            }

            Target = target;
            PreviewKind = previewKind;
            SystemKind = systemKind;
            ActionLabel = string.IsNullOrWhiteSpace(actionLabel)
                ? throw new ArgumentException("Action label is required.", nameof(actionLabel))
                : actionLabel.Trim();
            UnavailableStatus = string.IsNullOrWhiteSpace(unavailableStatus)
                ? throw new ArgumentException("Unavailable status is required.", nameof(unavailableStatus))
                : unavailableStatus.Trim();
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        }

        public CottonFileOpenTarget Target { get; }

        public CottonFilePreviewKind PreviewKind { get; }

        public CottonSystemFileOpenKind SystemKind { get; }

        public string ActionLabel { get; }

        public string UnavailableStatus { get; }

        public string? ContentType { get; }

        public bool CanPreviewInApp => Target == CottonFileOpenTarget.InAppPreview;

        public bool OpensWithSystemApp => Target == CottonFileOpenTarget.SystemApp;
    }
}
