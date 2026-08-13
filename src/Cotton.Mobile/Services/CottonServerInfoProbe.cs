// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Net.Http.Json;
using System.Text.Json;
using Cotton;
using Cotton.Models;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonServerInfoProbe : ICottonInstanceProbe
    {
        private static readonly HttpClient HttpClientInstance = new();
        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(10);

        private const string ServerInfoPath = Routes.V1.Server + "/info";

        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ILogger<CottonServerInfoProbe> _logger;

        public CottonServerInfoProbe(
            ICottonMobileApplicationMetadata metadata,
            ILogger<CottonServerInfoProbe> logger)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(logger);

            _metadata = metadata;
            _logger = logger;
        }

        public async Task<bool> IsCottonInstanceAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ProbeTimeout);
            using HttpRequestMessage request = new(HttpMethod.Get, CreateServerInfoUri(instanceUri));
            request.Headers.UserAgent.ParseAdd(_metadata.UserAgent);

            try
            {
                using HttpResponseMessage response = await HttpClientInstance
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                PublicServerInfo? serverInfo = await response.Content
                    .ReadFromJsonAsync<PublicServerInfo>(cancellationToken: timeout.Token)
                    .ConfigureAwait(false);
                return string.Equals(serverInfo?.Product, Constants.ProductName, StringComparison.Ordinal);
            }
            catch (HttpRequestException exception)
            {
                CottonLog.Debug(_logger, "Cotton instance probe request failed.", exception);
                return false;
            }
            catch (JsonException exception)
            {
                CottonLog.Debug(_logger, "Cotton instance probe returned an invalid response.", exception);
                return false;
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                CottonLog.Debug(_logger, "Cotton instance probe timed out.", exception);
                return false;
            }
        }

        private static Uri CreateServerInfoUri(Uri instanceUri)
        {
            string basePath = instanceUri.AbsolutePath.TrimEnd('/');
            UriBuilder builder = new(instanceUri)
            {
                Path = basePath + ServerInfoPath,
                Query = string.Empty,
            };
            return builder.Uri;
        }
    }
}
