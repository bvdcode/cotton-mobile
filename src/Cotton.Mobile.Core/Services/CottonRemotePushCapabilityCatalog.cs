namespace Cotton.Mobile.Services
{
    public static class CottonRemotePushCapabilityCatalog
    {
        private static readonly CottonRemotePushServerCapabilityKind[] RequiredServerCapabilities =
        {
            CottonRemotePushServerCapabilityKind.DeviceTokenRegistrationEndpoint,
            CottonRemotePushServerCapabilityKind.DeviceTokenRefreshUpsert,
            CottonRemotePushServerCapabilityKind.LogoutTokenRevocation,
            CottonRemotePushServerCapabilityKind.ServerEventCategories,
            CottonRemotePushServerCapabilityKind.PushPayloadPrivacyFiltering,
            CottonRemotePushServerCapabilityKind.StaleTokenCleanup,
            CottonRemotePushServerCapabilityKind.UserNotificationPreferences,
            CottonRemotePushServerCapabilityKind.ProviderDeliveryService,
        };

        private static readonly CottonRemotePushServerCapabilityKind[] CurrentBackendCapabilities =
        {
            CottonRemotePushServerCapabilityKind.DeviceTokenRegistrationEndpoint,
            CottonRemotePushServerCapabilityKind.DeviceTokenRefreshUpsert,
            CottonRemotePushServerCapabilityKind.LogoutTokenRevocation,
            CottonRemotePushServerCapabilityKind.ServerEventCategories,
            CottonRemotePushServerCapabilityKind.PushPayloadPrivacyFiltering,
            CottonRemotePushServerCapabilityKind.StaleTokenCleanup,
            CottonRemotePushServerCapabilityKind.UserNotificationPreferences,
            CottonRemotePushServerCapabilityKind.ProviderDeliveryService,
        };

        private static readonly CottonRemotePushEventCategorySnapshot[] EventCategories =
        {
            new CottonRemotePushEventCategorySnapshot(
                CottonRemotePushEventCategory.SharedFile,
                CottonNotificationChannelKind.Shares,
                defaultEnabled: false,
                requiresServerNotificationRow: true),
            new CottonRemotePushEventCategorySnapshot(
                CottonRemotePushEventCategory.AccessRequest,
                CottonNotificationChannelKind.Shares,
                defaultEnabled: false,
                requiresServerNotificationRow: true),
            new CottonRemotePushEventCategorySnapshot(
                CottonRemotePushEventCategory.CommentMention,
                CottonNotificationChannelKind.Shares,
                defaultEnabled: false,
                requiresServerNotificationRow: true),
            new CottonRemotePushEventCategorySnapshot(
                CottonRemotePushEventCategory.SecuritySession,
                CottonNotificationChannelKind.Security,
                defaultEnabled: true,
                requiresServerNotificationRow: true),
        };

        public static CottonRemotePushCapabilitySnapshot AndroidClosedTestingCurrentBackend { get; } =
            new CottonRemotePushCapabilitySnapshot(
                CottonRemotePushProviderKind.FirebaseCloudMessaging,
                CottonRemotePushMobilePlatform.Android,
                RequiredServerCapabilities,
                CurrentBackendCapabilities,
                EventCategories,
                CottonRemotePushPayloadPrivacyPolicy.GenericVisiblePayloads,
                requiresAndroidPostNotificationsPermissionForVisibleAlerts: true);
    }
}
