// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class CloudFolderPickerService : ICloudFolderPickerService
    {
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly ILoggerFactory _loggerFactory;

        public CloudFolderPickerService(
            ICottonFileBrowserService fileBrowserService,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _fileBrowserService = fileBrowserService;
            _loggerFactory = loggerFactory;
        }

        public async Task<CottonUploadDestinationSnapshot?> PickAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<CottonUploadDestinationSnapshot?> completion = new TaskCompletionSource<CottonUploadDestinationSnapshot?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            CloudFolderPickerViewModel viewModel = new CloudFolderPickerViewModel(
                instanceUri,
                _fileBrowserService,
                destination => completion.TrySetResult(destination),
                _loggerFactory.CreateLogger<CloudFolderPickerViewModel>());
            CloudFolderPickerPage page = new CloudFolderPickerPage(viewModel);
            INavigation navigation = await MainThread.InvokeOnMainThreadAsync(GetNavigation);

            await MainThread.InvokeOnMainThreadAsync(
                () => navigation.PushModalAsync(page, animated: false));
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => completion.TrySetCanceled(cancellationToken));
            try
            {
                return await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (navigation.ModalStack.Contains(page))
                    {
                        await navigation.PopModalAsync(animated: false);
                    }
                });
            }
        }

        private static INavigation GetNavigation()
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation
                ?? throw new InvalidOperationException("Cloud folder picker needs an active page.");
        }
    }
}
