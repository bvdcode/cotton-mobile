// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushPreferenceService : ICottonRemotePushPreferenceService
    {
        private const string CurrentPreferencesRoute = Routes.V1.Users + "/me/preferences/push-notifications";

        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonRemotePushPreferenceService(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public Task<CottonRemotePushPreferences> GetCurrentAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            return _apiClient.SendJsonAsync<CottonRemotePushPreferences>(
                instanceUri,
                HttpMethod.Get,
                CurrentPreferencesRoute,
                cancellationToken);
        }

        public Task<CottonRemotePushPreferences> UpdateCurrentAsync(
            Uri instanceUri,
            CottonRemotePushPreferences preferences,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            return _apiClient.SendJsonAsync<CottonRemotePushPreferences>(
                instanceUri,
                HttpMethod.Patch,
                CurrentPreferencesRoute,
                preferences,
                cancellationToken);
        }
    }
}
