// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class SyncSettingsPageService : ISyncSettingsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public SyncSettingsPageService(IServiceProvider serviceProvider)
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
                if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is SyncSettingsPage currentPage
                    && currentPage.BindingContext is SyncSettingsViewModel currentViewModel)
                {
                    currentViewModel.Configure(instanceUri);
                    currentViewModel.LoadCommand.Execute(null);
                    return;
                }

                var page = ActivatorUtilities.CreateInstance<SyncSettingsPage>(_serviceProvider);
                if (page.BindingContext is SyncSettingsViewModel viewModel)
                {
                    viewModel.Configure(instanceUri);
                }

                await CottonShellNavigation.PushAsync(
                    page,
                    cancellationToken,
                    currentPage => currentPage is SyncSettingsPage);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
