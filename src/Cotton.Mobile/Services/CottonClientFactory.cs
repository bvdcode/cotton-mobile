// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonClientFactory : ICottonClientFactory
    {
        private readonly CottonAccessTokenStore _accessTokenStore;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ICottonMobileApplicationMetadata _metadata;

        public CottonClientFactory(
            CottonAccessTokenStore accessTokenStore,
            ILoggerFactory loggerFactory,
            ICottonMobileApplicationMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(accessTokenStore);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(metadata);

            _accessTokenStore = accessTokenStore;
            _loggerFactory = loggerFactory;
            _metadata = metadata;
        }

        public ICottonCloudClient Create(Uri instanceUri)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            CottonSdkOptions options = new()
            {
                BaseAddress = instanceUri,
                DeviceName = _metadata.DeviceName,
                UserAgent = _metadata.UserAgent,
            };

            return new CottonCloudClient(_accessTokenStore, options, _loggerFactory);
        }
    }
}
