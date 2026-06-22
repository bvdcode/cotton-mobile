// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
