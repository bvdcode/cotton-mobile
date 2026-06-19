using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Cotton;
using Cotton.Auth;
using Cotton.Sdk;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.Services
{
    public class CottonCloudShareLinkService : ICottonCloudShareLinkService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const int ResponsePreviewLength = 180;

        private readonly HttpClient _httpClient;
        private readonly ICottonTokenStore _tokenStore;
        private readonly CottonCloudShareLinkHttpOptions _options;
        private readonly SemaphoreSlim _refreshGate = new(1, 1);

        public CottonCloudShareLinkService(
            HttpClient httpClient,
            ICottonTokenStore tokenStore,
            CottonCloudShareLinkHttpOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(tokenStore);

            _httpClient = httpClient;
            _tokenStore = tokenStore;
            _options = options ?? CottonCloudShareLinkHttpOptions.Default;
        }

        public async Task<CottonCloudShareLinkSnapshot> CreateAsync(
            Uri instanceUri,
            CottonCloudShareLinkRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedInstanceUri(instanceUri);
            ArgumentNullException.ThrowIfNull(request);

            string path = CreateRoute(request);
            string backendLink = await SendJsonAsync<string>(
                    instanceUri,
                    HttpMethod.Get,
                    path,
                    cancellationToken)
                .ConfigureAwait(false);
            return CottonCloudShareLinkSnapshot.Create(request, instanceUri, backendLink);
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

        private async Task<T> SendJsonAsync<T>(
            Uri instanceUri,
            HttpMethod method,
            string path,
            CancellationToken cancellationToken)
        {
            (HttpResponseMessage response, string? requestAccessToken) =
                await SendOnceAsync(
                        instanceUri,
                        method,
                        path,
                        authorize: true,
                        cancellationToken)
                    .ConfigureAwait(false);
            using (response)
            {
                if (response.StatusCode != HttpStatusCode.Unauthorized || !_options.RefreshOnUnauthorized)
                {
                    return await ReadRequiredJsonAsync<T>(response, method, path, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            await TryRefreshAsync(instanceUri, requestAccessToken, cancellationToken).ConfigureAwait(false);

            (HttpResponseMessage retry, _) = await SendOnceAsync(
                    instanceUri,
                    method,
                    path,
                    authorize: true,
                    cancellationToken)
                .ConfigureAwait(false);
            using (retry)
            {
                return await ReadRequiredJsonAsync<T>(retry, method, path, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<(HttpResponseMessage Response, string? AccessToken)> SendOnceAsync(
            Uri instanceUri,
            HttpMethod method,
            string path,
            bool authorize,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = await CreateRequestAsync(
                    instanceUri,
                    method,
                    path,
                    authorize,
                    cancellationToken)
                .ConfigureAwait(false);
            string? accessToken = request.Headers.Authorization?.Parameter;
            try
            {
                HttpResponseMessage response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken)
                    .ConfigureAwait(false);
                return (response, accessToken);
            }
            finally
            {
                request.Dispose();
            }
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(
            Uri instanceUri,
            HttpMethod method,
            string path,
            bool authorize,
            CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(method, CreateUri(instanceUri, path));
            ApplyDefaultHeaders(request);
            if (authorize)
            {
                TokenPairDto? tokens = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(tokens?.AccessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
                }
            }

            return request;
        }

        private async Task<bool> TryRefreshAsync(
            Uri instanceUri,
            string? failedAccessToken,
            CancellationToken cancellationToken)
        {
            await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                TokenPairDto? tokens = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
                if (HasUsableAccessTokenChanged(failedAccessToken, tokens?.AccessToken))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(tokens?.RefreshToken))
                {
                    return false;
                }

                string path = Routes.V1.Auth + "/refresh?refreshToken=" + Uri.EscapeDataString(tokens.RefreshToken);
                (HttpResponseMessage response, _) = await SendOnceAsync(
                        instanceUri,
                        HttpMethod.Post,
                        path,
                        authorize: false,
                        cancellationToken)
                    .ConfigureAwait(false);
                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        await _tokenStore.ClearAsync(cancellationToken).ConfigureAwait(false);
                        return false;
                    }

                    TokenPairDto refreshed = await ReadRequiredJsonAsync<TokenPairDto>(
                            response,
                            HttpMethod.Post,
                            path,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await _tokenStore.SaveAsync(refreshed, cancellationToken).ConfigureAwait(false);
                    return true;
                }
            }
            finally
            {
                _refreshGate.Release();
            }
        }

        private static async Task<T> ReadRequiredJsonAsync<T>(
            HttpResponseMessage response,
            HttpMethod method,
            string path,
            CancellationToken cancellationToken)
        {
            await EnsureSuccessAsync(response, method, path, cancellationToken).ConfigureAwait(false);

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new CottonApiException(
                    response.StatusCode,
                    null,
                    $"Cotton API request {FormatRequestLabel(method, path)} returned an empty JSON response.");
            }

            try
            {
                if (typeof(T) == typeof(string)
                    && !body.TrimStart().StartsWith("\"", StringComparison.Ordinal))
                {
                    return (T)(object)body.Trim();
                }

                T? result = JsonSerializer.Deserialize<T>(body, JsonOptions);
                return result ?? throw new CottonApiException(
                    response.StatusCode,
                    null,
                    $"Cotton API request {FormatRequestLabel(method, path)} returned an empty JSON response.");
            }
            catch (JsonException exception)
            {
                throw new CottonApiException(
                    response.StatusCode,
                    body,
                    $"Cotton API request {FormatRequestLabel(method, path)} returned invalid JSON."
                    + " Response: "
                    + CreateResponsePreview(body),
                    exception);
            }
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            HttpMethod method,
            string path,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string? body = response.Content is null
                ? null
                : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string message = $"Cotton API request {FormatRequestLabel(method, path)} failed"
                + $" with status {(int)response.StatusCode} ({response.StatusCode}).";
            if (!string.IsNullOrWhiteSpace(body))
            {
                message += " Response: " + CreateResponsePreview(body);
            }

            throw new CottonApiException(response.StatusCode, body, message);
        }

        private void ApplyDefaultHeaders(HttpRequestMessage request)
        {
            if (!string.IsNullOrWhiteSpace(_options.UserAgent))
            {
                request.Headers.UserAgent.ParseAdd(_options.UserAgent);
            }

            string? deviceName = NormalizeDeviceName(_options.DeviceName);
            if (deviceName is not null)
            {
                request.Headers.TryAddWithoutValidation(CottonClientHeaders.DeviceName, deviceName);
            }
        }

        private static string? NormalizeDeviceName(string? value)
        {
            string? normalized = value?.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            return normalized.Length <= CottonClientHeaders.DeviceNameMaxLength
                ? normalized
                : normalized[..CottonClientHeaders.DeviceNameMaxLength];
        }

        private static Uri CreateUri(Uri instanceUri, string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri? absoluteUri) && IsHttpUri(absoluteUri))
            {
                return absoluteUri;
            }

            string relative = path.TrimStart('/');
            int queryIndex = relative.IndexOf('?', StringComparison.Ordinal);
            string relativePath = queryIndex >= 0
                ? relative[..queryIndex]
                : relative;
            string query = queryIndex >= 0
                ? relative[(queryIndex + 1)..]
                : string.Empty;
            string basePath = instanceUri.AbsolutePath.TrimEnd('/');
            string combinedPath = string.IsNullOrEmpty(basePath)
                ? "/" + relativePath
                : basePath + "/" + relativePath;

            var builder = new UriBuilder(instanceUri)
            {
                Path = combinedPath,
                Query = query,
                Fragment = string.Empty,
            };
            return builder.Uri;
        }

        private static void EnsureSupportedInstanceUri(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!instanceUri.IsAbsoluteUri
                || !string.Equals(instanceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(instanceUri.Host)
                || !string.IsNullOrWhiteSpace(instanceUri.UserInfo)
                || !string.IsNullOrWhiteSpace(instanceUri.Query)
                || !string.IsNullOrWhiteSpace(instanceUri.Fragment))
            {
                throw new ArgumentException(
                    "Cotton instance URL must be an absolute HTTPS URL.",
                    nameof(instanceUri));
            }
        }

        private static string FormatRequestLabel(HttpMethod method, string path)
        {
            return $"{method.Method} {RedactPath(path)}";
        }

        private static string CreateResponsePreview(string responseBody)
        {
            string preview = responseBody
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Trim();
            return preview.Length <= ResponsePreviewLength
                ? preview
                : preview[..ResponsePreviewLength] + "...";
        }

        private static string RedactPath(string path)
        {
            int queryIndex = path.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex < 0 || queryIndex == path.Length - 1)
            {
                return path;
            }

            string route = path[..queryIndex];
            string query = path[(queryIndex + 1)..];
            string[] parts = query.Split('&');
            for (int index = 0; index < parts.Length; index++)
            {
                string part = parts[index];
                int equalsIndex = part.IndexOf('=', StringComparison.Ordinal);
                string key = equalsIndex < 0 ? part : part[..equalsIndex];
                if (string.Equals(key, "token", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "refreshToken", StringComparison.OrdinalIgnoreCase))
                {
                    parts[index] = key + "=***";
                }
            }

            return route + "?" + string.Join("&", parts);
        }

        private static bool HasUsableAccessTokenChanged(string? failedAccessToken, string? currentAccessToken)
        {
            return !string.IsNullOrWhiteSpace(currentAccessToken)
                && !string.Equals(currentAccessToken, failedAccessToken, StringComparison.Ordinal);
        }

        private static bool IsHttpUri(Uri uri)
        {
            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }
    }
}
