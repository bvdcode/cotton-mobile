// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushCapabilitySnapshot
    {
        public CottonRemotePushCapabilitySnapshot(
            CottonRemotePushProviderKind provider,
            CottonRemotePushMobilePlatform platform,
            IReadOnlyList<CottonRemotePushServerCapabilityKind> requiredServerCapabilities,
            IReadOnlyList<CottonRemotePushServerCapabilityKind> availableServerCapabilities,
            IReadOnlyList<CottonRemotePushEventCategorySnapshot> eventCategories,
            CottonRemotePushPayloadPrivacyPolicy payloadPrivacyPolicy,
            bool requiresAndroidPostNotificationsPermissionForVisibleAlerts)
        {
            ArgumentNullException.ThrowIfNull(requiredServerCapabilities);
            ArgumentNullException.ThrowIfNull(availableServerCapabilities);
            ArgumentNullException.ThrowIfNull(eventCategories);
            ArgumentNullException.ThrowIfNull(payloadPrivacyPolicy);

            if (!Enum.IsDefined(provider))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(provider),
                    "Remote push provider is not supported.");
            }

            if (!Enum.IsDefined(platform))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(platform),
                    "Remote push mobile platform is not supported.");
            }

            CottonRemotePushEventCategorySnapshot[] categorySnapshots = eventCategories.ToArray();
            if (categorySnapshots.Any(category => category is null))
            {
                throw new ArgumentException(
                    "Remote push visible event categories cannot contain null entries.",
                    nameof(eventCategories));
            }

            if (categorySnapshots
                .GroupBy(category => category.Category)
                .Any(group => group.Count() > 1))
            {
                throw new ArgumentException(
                    "Remote push visible event categories must be unique.",
                    nameof(eventCategories));
            }

            Provider = provider;
            Platform = platform;
            RequiredServerCapabilities = requiredServerCapabilities
                .Distinct()
                .OrderBy(capability => capability)
                .ToArray();
            AvailableServerCapabilities = availableServerCapabilities
                .Distinct()
                .OrderBy(capability => capability)
                .ToArray();
            EventCategories = categorySnapshots;
            PayloadPrivacyPolicy = payloadPrivacyPolicy;
            RequiresAndroidPostNotificationsPermissionForVisibleAlerts =
                requiresAndroidPostNotificationsPermissionForVisibleAlerts;
        }

        public CottonRemotePushProviderKind Provider { get; }

        public CottonRemotePushMobilePlatform Platform { get; }

        public IReadOnlyList<CottonRemotePushServerCapabilityKind> RequiredServerCapabilities { get; }

        public IReadOnlyList<CottonRemotePushServerCapabilityKind> AvailableServerCapabilities { get; }

        public IReadOnlyList<CottonRemotePushEventCategorySnapshot> EventCategories { get; }

        public CottonRemotePushPayloadPrivacyPolicy PayloadPrivacyPolicy { get; }

        public bool RequiresAndroidPostNotificationsPermissionForVisibleAlerts { get; }

        public IReadOnlyList<CottonRemotePushServerCapabilityKind> MissingServerCapabilities =>
            RequiredServerCapabilities
                .Except(AvailableServerCapabilities)
                .OrderBy(capability => capability)
                .ToArray();

        public bool CanRegisterDeviceToken =>
            !MissingServerCapabilities.Contains(
                CottonRemotePushServerCapabilityKind.DeviceTokenRegistrationEndpoint)
            && !MissingServerCapabilities.Contains(
                CottonRemotePushServerCapabilityKind.DeviceTokenRefreshUpsert);

        public bool CanDeliverRemotePush => MissingServerCapabilities.Count == 0;

        public CottonRemotePushEventCategorySnapshot? FindVisibleEventCategory(
            CottonRemotePushEventCategory category)
        {
            return Enum.IsDefined(category)
                ? EventCategories.SingleOrDefault(snapshot => snapshot.Category == category)
                : null;
        }

        public bool SupportsVisibleEventCategory(CottonRemotePushEventCategory category)
        {
            return FindVisibleEventCategory(category) is not null;
        }
    }
}
