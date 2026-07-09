// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class CaptureInboxPageService : ICaptureInboxPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public CaptureInboxPageService(IServiceProvider serviceProvider)
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
                if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is CaptureInboxPage currentPage
                    && currentPage.BindingContext is CaptureInboxViewModel currentViewModel)
                {
                    currentViewModel.LoadCommand.Execute(null);
                    return;
                }

                var viewModel = ActivatorUtilities.CreateInstance<CaptureInboxViewModel>(
                    _serviceProvider,
                    instanceUri);
                var page = ActivatorUtilities.CreateInstance<CaptureInboxPage>(_serviceProvider, viewModel);
                await CottonShellNavigation.PushAsync(page, cancellationToken);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
