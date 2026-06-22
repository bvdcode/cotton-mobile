// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonAccountSessionService : ICottonAccountSessionService
    {
        private const string SessionsRoute = Routes.V1.Auth + "/sessions";

        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonAccountSessionService(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task<IReadOnlyList<CottonAccountSessionSnapshot>> GetActiveSessionsAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            List<SessionResponse> response = await _apiClient
                .SendJsonAsync<List<SessionResponse>>(
                    instanceUri,
                    HttpMethod.Get,
                    SessionsRoute,
                    cancellationToken)
                .ConfigureAwait(false);

            return response
                .Select(session => session.ToSnapshot())
                .ToArray();
        }

        public Task RevokeSessionAsync(
            Uri instanceUri,
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

            string path = SessionsRoute + "/" + Uri.EscapeDataString(sessionId.Trim());
            return _apiClient.SendRequiredAsync(
                instanceUri,
                HttpMethod.Delete,
                path,
                cancellationToken);
        }

        private class SessionResponse
        {
            public string SessionId { get; set; } = string.Empty;

            public string? Device { get; set; }

            public string? IpAddress { get; set; }

            public string? UserAgent { get; set; }

            public int AuthType { get; set; }

            public string? Country { get; set; }

            public string? Region { get; set; }

            public string? City { get; set; }

            public int RefreshTokenCount { get; set; }

            public TimeSpan TotalSessionDuration { get; set; }

            public bool IsCurrentSession { get; set; }

            public DateTime LastSeenAt { get; set; }

            public CottonAccountSessionSnapshot ToSnapshot()
            {
                return new CottonAccountSessionSnapshot(
                    SessionId,
                    Device,
                    IpAddress,
                    UserAgent,
                    AuthType,
                    Country,
                    Region,
                    City,
                    RefreshTokenCount,
                    TotalSessionDuration,
                    IsCurrentSession,
                    LastSeenAt);
            }
        }
    }
}
