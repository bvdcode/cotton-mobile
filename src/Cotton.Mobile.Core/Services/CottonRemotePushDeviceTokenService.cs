// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDeviceTokenService : ICottonRemotePushDeviceTokenService
    {
        private const string CurrentTokenRoute = Routes.V1.Notifications + "/device-tokens/current";
        private const string CurrentSessionRoute = Routes.V1.Notifications + "/device-tokens/current-session";

        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonRemotePushDeviceTokenService(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task<CottonRemotePushDeviceTokenSnapshot> RegisterCurrentAsync(
            Uri instanceUri,
            CottonRemotePushDeviceTokenRegistrationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            PushDeviceTokenResponse response = await _apiClient.SendJsonAsync<PushDeviceTokenResponse>(
                    instanceUri,
                    HttpMethod.Put,
                    CurrentTokenRoute,
                    request,
                    cancellationToken)
                .ConfigureAwait(false);
            return response.ToSnapshot();
        }

        public async Task<CottonRemotePushDeviceTokenRevocationResult> RevokeCurrentSessionAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            PushDeviceTokenRevocationResponse response =
                await _apiClient.SendJsonAsync<PushDeviceTokenRevocationResponse>(
                        instanceUri,
                        HttpMethod.Delete,
                        CurrentSessionRoute,
                        cancellationToken)
                    .ConfigureAwait(false);
            return response.ToResult();
        }

        private class PushDeviceTokenResponse
        {
            public Guid Id { get; set; }

            public CottonRemotePushProviderKind Provider { get; set; }

            public CottonRemotePushMobilePlatform Platform { get; set; }

            public string SessionId { get; set; } = string.Empty;

            public string? DeviceName { get; set; }

            public string? AppVersion { get; set; }

            public DateTime LastRegisteredAt { get; set; }

            public DateTime? RevokedAt { get; set; }

            public CottonRemotePushDeviceTokenSnapshot ToSnapshot()
            {
                return new CottonRemotePushDeviceTokenSnapshot(
                    Id,
                    Provider,
                    Platform,
                    SessionId,
                    DeviceName,
                    AppVersion,
                    LastRegisteredAt,
                    RevokedAt);
            }
        }

        private class PushDeviceTokenRevocationResponse
        {
            public int RevokedTokens { get; set; }

            public CottonRemotePushDeviceTokenRevocationResult ToResult()
            {
                return new CottonRemotePushDeviceTokenRevocationResult(RevokedTokens);
            }
        }
    }
}
