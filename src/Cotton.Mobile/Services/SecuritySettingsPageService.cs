// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class SecuritySettingsPageService : ISecuritySettingsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public SecuritySettingsPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public async Task OpenAsync(
            ICottonCurrentSessionRevocationHandler revocationHandler,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(revocationHandler);

            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is SecuritySettingsPage currentPage
                    && currentPage.BindingContext is SecuritySettingsViewModel currentViewModel)
                {
                    currentViewModel.SetCurrentSessionRevocationHandler(revocationHandler);
                    currentViewModel.LoadCommand.Execute(null);
                    return;
                }

                var page = ActivatorUtilities.CreateInstance<SecuritySettingsPage>(_serviceProvider);
                if (page.BindingContext is SecuritySettingsViewModel viewModel)
                {
                    viewModel.SetCurrentSessionRevocationHandler(revocationHandler);
                }

                await CottonShellNavigation.PushAsync(
                    page,
                    cancellationToken,
                    currentPage => currentPage is SecuritySettingsPage);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
