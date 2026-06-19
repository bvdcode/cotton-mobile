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
            EventCategories = eventCategories.ToArray();
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
    }
}
