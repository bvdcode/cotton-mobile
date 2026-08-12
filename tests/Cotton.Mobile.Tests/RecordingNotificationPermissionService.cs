// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingNotificationPermissionService(Exception? requestFailure = null) :
        ICottonNotificationPermissionService
    {
        public int RequestCount { get; private set; }

        public Task<bool> CanPostAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task RequestIfNeededAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            if (requestFailure is not null)
            {
                return Task.FromException(requestFailure);
            }

            return Task.CompletedTask;
        }
    }
}
