namespace Cotton.Mobile.Services
{
    public enum CottonRemotePushServerCapabilityKind
    {
        DeviceTokenRegistrationEndpoint = 0,
        DeviceTokenRefreshUpsert = 1,
        LogoutTokenRevocation = 2,
        ServerEventCategories = 3,
        PushPayloadPrivacyFiltering = 4,
        StaleTokenCleanup = 5,
        UserNotificationPreferences = 6,
        ProviderDeliveryService = 7,
    }
}
