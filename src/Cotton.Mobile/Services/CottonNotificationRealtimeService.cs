// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;
using Cotton.Sdk.Realtime;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonNotificationRealtimeService : ICottonNotificationRealtimeService
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonNotificationPollingService _pollingService;
        private readonly ILogger<CottonNotificationRealtimeService> _logger;

        private ICottonCloudClient? _client;
        private Uri? _instanceUri;

        public CottonNotificationRealtimeService(
            ICottonClientFactory clientFactory,
            ICottonNotificationPollingService pollingService,
            ILogger<CottonNotificationRealtimeService> logger)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(pollingService);
            ArgumentNullException.ThrowIfNull(logger);

            _clientFactory = clientFactory;
            _pollingService = pollingService;
            _logger = logger;
        }

        public async Task StartAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_client is not null && Uri.Equals(_instanceUri, instanceUri))
                {
                    return;
                }

                await StopCoreAsync().ConfigureAwait(false);

                ICottonCloudClient client = _clientFactory.Create(instanceUri);
                client.Realtime.NotificationReceived += OnNotificationReceived;
                try
                {
                    await client.Realtime.StartAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    CottonLog.Warning(_logger, "Failed to establish the Cotton realtime notification connection.", exception);
                    client.Realtime.NotificationReceived -= OnNotificationReceived;
                    await client.DisposeAsync().ConfigureAwait(false);
                    throw;
                }

                _client = client;
                _instanceUri = instanceUri;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await StopCoreAsync().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _gate.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task StopCoreAsync()
        {
            ICottonCloudClient? client = _client;
            _client = null;
            _instanceUri = null;
            if (client is null)
            {
                return;
            }

            client.Realtime.NotificationReceived -= OnNotificationReceived;
            await client.DisposeAsync().ConfigureAwait(false);
        }

        private void OnNotificationReceived(object? sender, CottonRealtimeEvent notificationEvent)
        {
            _ = PollSafelyAsync();
        }

        private async Task PollSafelyAsync()
        {
            try
            {
                await _pollingService.CheckAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                CottonLog.Warning(_logger, "Failed to fetch Cotton notifications after a realtime event.", exception);
            }
        }
    }
}
