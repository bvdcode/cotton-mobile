using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonAccountSessionService : ICottonAccountSessionService
    {
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
                    Routes.V1.Auth + "/sessions",
                    cancellationToken)
                .ConfigureAwait(false);

            return response
                .Select(session => session.ToSnapshot())
                .ToArray();
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
