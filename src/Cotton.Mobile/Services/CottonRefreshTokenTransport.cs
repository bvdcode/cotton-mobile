// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Net.Http.Json;
using Cotton.Auth;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonRefreshTokenTransport(HttpClient httpClient) : ICottonRefreshTokenTransport
    {
        private const string RefreshPath = "api/v1/auth/refresh";
        private const string LogoutPath = "api/v1/auth/logout";
        private const string RefreshTokenCookieName = "refresh_token";

        private readonly HttpClient _httpClient =
            httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        public Task<TokenPairDto> RefreshAsync(
            Uri instanceUri,
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<TokenPairDto>(
                instanceUri,
                RefreshPath,
                refreshToken,
                cancellationToken);
        }

        public async Task LogoutAsync(
            Uri instanceUri,
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            using HttpRequestMessage request = CreateRequest(instanceUri, LogoutPath, refreshToken);
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> SendAsync<T>(
            Uri instanceUri,
            string path,
            string refreshToken,
            CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = CreateRequest(instanceUri, path, refreshToken);
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            T? result = await response.Content
                .ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return result ?? throw new CottonApiException(
                response.StatusCode,
                null,
                "Cotton refresh-token endpoint returned an empty response.");
        }

        private static HttpRequestMessage CreateRequest(
            Uri instanceUri,
            string path,
            string refreshToken)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
            string baseUri = instanceUri.AbsoluteUri.TrimEnd('/') + "/";
            Uri requestUri = new(new Uri(baseUri), path);
            HttpRequestMessage request = new(HttpMethod.Post, requestUri);
            request.Headers.Add("Cookie", $"{RefreshTokenCookieName}={refreshToken}");
            return request;
        }

        private static async Task EnsureSuccessAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string body = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            throw new CottonApiException(
                response.StatusCode,
                body,
                $"Cotton refresh-token endpoint failed with status {(int)response.StatusCode}.");
        }
    }
}
