// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class TrashPageService : ITrashPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public TrashPageService(IServiceProvider serviceProvider)
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
                if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is TrashPage currentPage
                    && currentPage.BindingContext is TrashViewModel currentViewModel)
                {
                    currentViewModel.LoadCommand.Execute(null);
                    return;
                }

                var viewModel = ActivatorUtilities.CreateInstance<TrashViewModel>(
                    _serviceProvider,
                    instanceUri);
                var page = ActivatorUtilities.CreateInstance<TrashPage>(_serviceProvider, viewModel);
                await CottonShellNavigation.PushAsync(
                    page,
                    cancellationToken,
                    currentPage => currentPage is TrashPage);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
