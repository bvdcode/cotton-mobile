// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class StorageSettingsPageService : IStorageSettingsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public StorageSettingsPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = ActivatorUtilities.CreateInstance<StoragePage>(_serviceProvider);
                await Shell.Current.Navigation.PushAsync(page);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
