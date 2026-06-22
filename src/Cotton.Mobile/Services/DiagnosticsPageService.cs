// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class DiagnosticsPageService : IDiagnosticsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public DiagnosticsPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public async Task OpenAsync(CottonDiagnosticsContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var viewModel = ActivatorUtilities.CreateInstance<DiagnosticsViewModel>(
                    _serviceProvider,
                    context);
                var page = ActivatorUtilities.CreateInstance<DiagnosticsPage>(
                    _serviceProvider,
                    viewModel);
                await Shell.Current.Navigation.PushAsync(page);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
