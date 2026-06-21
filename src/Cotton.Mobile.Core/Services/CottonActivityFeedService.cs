using System.Globalization;
using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonActivityFeedService : ICottonActivityFeedService
    {
        private const string NotificationsRoute = Routes.V1.Notifications;
        private const string TotalCountHeaderName = "X-Total-Count";

        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonActivityFeedService(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task<CottonActivityFeedPageSnapshot> GetPageAsync(
            Uri instanceUri,
            CottonActivityFeedQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            string route = NotificationsRoute + "?" + query.ToQueryString();
            CottonAuthenticatedApiResponse<List<NotificationResponse>> response = await _apiClient
                .SendJsonResponseAsync<List<NotificationResponse>>(
                    instanceUri,
                    HttpMethod.Get,
                    route,
                    cancellationToken)
                .ConfigureAwait(false);

            return new CottonActivityFeedPageSnapshot(
                query,
                response.Value.Select(item => item.ToSnapshot()).ToArray(),
                ReadTotalItemCount(response));
        }

        private static int? ReadTotalItemCount(
            CottonAuthenticatedApiResponse<List<NotificationResponse>> response)
        {
            string? header = response.GetHeaderValue(TotalCountHeaderName);
            if (string.IsNullOrWhiteSpace(header))
            {
                return null;
            }

            return int.TryParse(
                header.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int totalItemCount)
                && totalItemCount >= 0
                    ? totalItemCount
                    : null;
        }

        private class NotificationResponse
        {
            public Guid Id { get; set; }

            public string Title { get; set; } = string.Empty;

            public string? Content { get; set; }

            public DateTime CreatedAt { get; set; }

            public DateTime? ReadAt { get; set; }

            public CottonActivityFeedPriority Priority { get; set; }

            public Dictionary<string, string>? Metadata { get; set; }

            public CottonActivityFeedItemSnapshot ToSnapshot()
            {
                return new CottonActivityFeedItemSnapshot(
                    Id,
                    Title,
                    Content,
                    CreatedAt,
                    ReadAt,
                    Priority,
                    Metadata);
            }
        }
    }
}
