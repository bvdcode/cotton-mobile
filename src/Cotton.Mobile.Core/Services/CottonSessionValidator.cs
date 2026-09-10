// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;
using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public class CottonSessionValidator : ICottonSessionValidator
    {
        private readonly ICottonClientFactory _clientFactory;

        public CottonSessionValidator(ICottonClientFactory clientFactory)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);

            _clientFactory = clientFactory;
        }

        public async Task<UserDto> ValidateAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            return await client.Auth.MeAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
