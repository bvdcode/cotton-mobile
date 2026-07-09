// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class FileVersionHistoryPageService : IFileVersionHistoryPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public FileVersionHistoryPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public async Task OpenAsync(
            Uri instanceUri,
            CottonFileBrowserEntry file,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is FileVersionHistoryPage currentPage
                    && currentPage.BindingContext is FileVersionHistoryViewModel currentViewModel
                    && currentViewModel.FileId == file.Id)
                {
                    currentViewModel.LoadCommand.Execute(null);
                    return;
                }

                var viewModel = ActivatorUtilities.CreateInstance<FileVersionHistoryViewModel>(
                    _serviceProvider,
                    instanceUri,
                    file);
                var page = ActivatorUtilities.CreateInstance<FileVersionHistoryPage>(_serviceProvider, viewModel);
                await CottonShellNavigation.PushAsync(page, cancellationToken);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
