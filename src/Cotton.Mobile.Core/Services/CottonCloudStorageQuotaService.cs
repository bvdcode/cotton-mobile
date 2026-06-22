// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton;

namespace Cotton.Mobile.Services
{
    public class CottonCloudStorageQuotaService : ICottonCloudStorageQuotaService
    {
        private const string CurrentStorageQuotaRoute = Routes.V1.Users + "/me/storage-quota";

        private readonly CottonAuthenticatedApiClient _apiClient;

        public CottonCloudStorageQuotaService(CottonAuthenticatedApiClient apiClient)
        {
            ArgumentNullException.ThrowIfNull(apiClient);

            _apiClient = apiClient;
        }

        public async Task<CottonCloudStorageQuotaSnapshot> GetCurrentAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            CottonUserStorageQuotaDto quota = await _apiClient
                .SendJsonAsync<CottonUserStorageQuotaDto>(
                    instanceUri,
                    HttpMethod.Get,
                    CurrentStorageQuotaRoute,
                    cancellationToken)
                .ConfigureAwait(false);

            return CottonCloudStorageQuotaSnapshot.Create(quota.UsedBytes, quota.QuotaBytes);
        }
    }
}
