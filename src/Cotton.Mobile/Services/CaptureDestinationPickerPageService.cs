// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class CaptureDestinationPickerPageService : ICaptureDestinationPickerPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public CaptureDestinationPickerPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public async Task OpenAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var viewModel = ActivatorUtilities.CreateInstance<CaptureDestinationPickerViewModel>(
                    _serviceProvider,
                    instanceUri);
                var page = ActivatorUtilities.CreateInstance<CaptureDestinationPickerPage>(
                    _serviceProvider,
                    viewModel);
                await CottonShellNavigation.PushAsync(page, cancellationToken);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
