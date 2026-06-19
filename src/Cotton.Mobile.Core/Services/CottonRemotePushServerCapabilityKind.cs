namespace Cotton.Mobile.Services
{
    public enum CottonRemotePushServerCapabilityKind
    {
        DeviceTokenRegistrationEndpoint = 0,
        DeviceTokenRefreshUpsert = 1,
        LogoutTokenRevocation = 2,
        StaleTokenCleanup = 3,
        UserNotificationPreferences = 4,
        PushPayloadPrivacyFiltering = 5,
    }
}
