using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonCloudShareLinkService : ICottonCloudShareLinkService
    {
        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonCloudShareLinkService(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task<CottonCloudShareLinkSnapshot> CreateAsync(
            Uri instanceUri,
            CottonCloudShareLinkRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            string path = CreateRoute(request);
            string backendLink = await _apiClient.SendJsonAsync<string>(
                    instanceUri,
                    HttpMethod.Get,
                    path,
                    cancellationToken)
                .ConfigureAwait(false);
            return CottonCloudShareLinkSnapshot.Create(request, instanceUri, backendLink);
        }

        public async Task InvalidateAllFileLinksAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            await _apiClient.SendRequiredAsync(
                    instanceUri,
                    HttpMethod.Post,
                    Routes.V1.Auth + "/invalidate-share-links",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private static string CreateRoute(CottonCloudShareLinkRequest request)
        {
            string expireQuery = "?expireAfterMinutes=" + request.ExpireAfterMinutes;
            return request.TargetKind switch
            {
                CottonCloudShareLinkTargetKind.File =>
                    $"{Routes.V1.Files}/{request.TargetId}/download-link{expireQuery}",
                CottonCloudShareLinkTargetKind.Folder =>
                    $"{Routes.V1.Layouts}/nodes/{request.TargetId}/share-link{expireQuery}",
                _ => throw new ArgumentOutOfRangeException(nameof(request), "Unsupported share link target kind."),
            };
        }
    }
}
