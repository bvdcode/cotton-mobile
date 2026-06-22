// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushPayloadPrivacyPolicy
    {
        public CottonRemotePushPayloadPrivacyPolicy(
            bool requiresNotificationId,
            bool requiresEventCategory,
            bool allowsFileNames,
            bool allowsFolderNames,
            bool allowsAccountIdentifiers,
            bool allowsPublicShareTokens)
        {
            RequiresNotificationId = requiresNotificationId;
            RequiresEventCategory = requiresEventCategory;
            AllowsFileNames = allowsFileNames;
            AllowsFolderNames = allowsFolderNames;
            AllowsAccountIdentifiers = allowsAccountIdentifiers;
            AllowsPublicShareTokens = allowsPublicShareTokens;
        }

        public static CottonRemotePushPayloadPrivacyPolicy GenericVisiblePayloads { get; } =
            new CottonRemotePushPayloadPrivacyPolicy(
                requiresNotificationId: true,
                requiresEventCategory: true,
                allowsFileNames: false,
                allowsFolderNames: false,
                allowsAccountIdentifiers: false,
                allowsPublicShareTokens: false);

        public bool RequiresNotificationId { get; }

        public bool RequiresEventCategory { get; }

        public bool AllowsFileNames { get; }

        public bool AllowsFolderNames { get; }

        public bool AllowsAccountIdentifiers { get; }

        public bool AllowsPublicShareTokens { get; }
    }
}
