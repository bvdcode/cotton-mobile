// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonClientFactory : ICottonClientFactory
    {
        private readonly ICottonTokenStore _tokenStore;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ICottonMobileApplicationMetadata _metadata;

        public CottonClientFactory(
            ICottonTokenStore tokenStore,
            ILoggerFactory loggerFactory,
            ICottonMobileApplicationMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(tokenStore);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(metadata);

            _tokenStore = tokenStore;
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

            return new CottonCloudClient(_tokenStore, options, _loggerFactory);
        }
    }
}
