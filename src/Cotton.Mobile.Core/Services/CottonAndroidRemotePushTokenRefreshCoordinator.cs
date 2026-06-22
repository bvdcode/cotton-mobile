// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAndroidRemotePushTokenRefreshCoordinator
    {
        private readonly ICottonAndroidRemotePushTokenRefreshHost _host;

        public CottonAndroidRemotePushTokenRefreshCoordinator(
            ICottonAndroidRemotePushTokenRefreshHost host)
        {
            ArgumentNullException.ThrowIfNull(host);

            _host = host;
        }

        public async Task<CottonAndroidRemotePushTokenRefreshScheduleResult> ScheduleAsync(
            CancellationToken cancellationToken = default)
        {
            CottonAndroidRemotePushTokenRefreshRequest request =
                CottonAndroidRemotePushTokenRefreshRequest.CreateDefault();
            return await _host.ScheduleAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CottonAndroidRemotePushTokenRefreshCancelResult> CancelAsync(
            CancellationToken cancellationToken = default)
        {
            var identity = new CottonAndroidRemotePushTokenRefreshScheduleIdentity();
            return await _host.CancelAsync(identity, cancellationToken).ConfigureAwait(false);
        }
    }
}
